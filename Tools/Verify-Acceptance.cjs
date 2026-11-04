// Local WebGL acceptance player only. Inputs use real browser mouse/keyboard events.
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const { spawn } = require('node:child_process');
const { createHash } = require('node:crypto');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const root = path.resolve('Builds/AcceptanceWebGL');
const output = path.resolve('Builds/Verification/acceptance-' + Date.now());
fs.mkdirSync(output, { recursive: true });
const report = { started: new Date().toISOString(), viewport: null, checks: [], captures: [], states: [], errors: [] };
let server, browser, ws, width = 1280, height = 720, lastState, captureFailure;
const messages = [];
const selected = process.argv.find(a => a.startsWith('--case='))?.split('=')[1];
const check = (ok, label) => { report.checks.push({ label, passed: !!ok }); console.log((ok ? 'PASS ' : 'FAIL ') + label); if (!ok) throw Error(label); };

(async () => {
  try {
    const buildFile = path.join(root, 'build-result.json');
    report.build = JSON.parse(fs.readFileSync(buildFile));
    if (!report.build.success) throw Error('Acceptance build has not succeeded.');
    report.build.completedUtc = fs.statSync(buildFile).mtime.toISOString();
    report.build.assets = Object.fromEntries(fs.readdirSync(path.join(root, 'Build')).map(name =>
      [name, createHash('sha256').update(fs.readFileSync(path.join(root, 'Build', name))).digest('hex')]));
    server = http.createServer((req, res) => {
      let file = path.resolve(root, '.' + new URL(req.url, 'http://localhost').pathname);
      if (file !== root && !file.startsWith(root + path.sep)) return res.writeHead(403).end();
      if (file === root) file = path.join(root, 'index.html');
      fs.readFile(file, (error, bytes) => {
        if (error) return res.writeHead(404).end();
        if (file.endsWith('index.html')) bytes = Buffer.from(bytes.toString()
          .replace('.then((unityInstance) => {', '.then((unityInstance) => { window.unityInstance=unityInstance;')
          .replace('let fullscreenContainer =', `// Coalesce overlapping native requests caused by synchronous test-fixture setup.
            const nativeLock = canvas.requestPointerLock.bind(canvas); let pendingLock;
            canvas.requestPointerLock = (...args) => {
              if (document.pointerLockElement === canvas) return Promise.resolve();
              if (pendingLock) return pendingLock;
              pendingLock = Promise.resolve(nativeLock(...args)).finally(() => { pendingLock = null; });
              return pendingLock;
            };
            let fullscreenContainer =`));
        res.setHeader('Content-Type', ({ '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.png': 'image/png' })[path.extname(file)] || 'application/octet-stream');
        res.end(bytes);
      });
    });
    await new Promise(resolve => server.listen(8878, '127.0.0.1', resolve));
    const cache = path.join(process.env.LOCALAPPDATA, 'ms-playwright');
    const chromium = fs.readdirSync(cache).find(p => /^chromium-\d+$/.test(p));
    const executable = process.env.WEBGL_BROWSER || path.join(cache, chromium, 'chrome-win64/chrome.exe');
    browser = spawn(executable, ['--headless=new', '--remote-debugging-port=9334', '--user-data-dir=' + path.join(output, 'browser'), '--no-first-run', '--no-default-browser-check', '--window-size=1280,800', 'about:blank'], { windowsHide: true, stdio: 'ignore' });
    let tab;
    for (let i = 0; i < 60 && !tab; i++) { try { tab = (await (await fetch('http://127.0.0.1:9334/json')).json()).find(t => t.type === 'page'); } catch {} if (!tab) await sleep(250); }
    if (!tab) throw Error('Browser failed to start.');
    ws = new WebSocket(tab.webSocketDebuggerUrl);
    await new Promise(resolve => { ws.onopen = resolve; });
    let id = 0;
    const pending = new Map();
    const send = (method, params = {}) => new Promise((resolve, reject) => {
      const key = ++id;
      const timer = setTimeout(() => { pending.delete(key); reject(Error('CDP timeout: ' + method)); }, 20000);
      pending.set(key, { resolve, reject, timer });
      ws.send(JSON.stringify({ id: key, method, params }));
    });
    ws.onmessage = event => {
      const value = JSON.parse(event.data);
      if (value.id) {
        const p = pending.get(value.id); if (!p) return;
        clearTimeout(p.timer); pending.delete(value.id); value.error ? p.reject(value.error) : p.resolve(value.result);
      } else if (value.method === 'Runtime.consoleAPICalled') {
        const text = value.params.args.map(a => a.value ?? a.description ?? '').join(' ');
        messages.push({ type: value.params.type, text });
        const at = text.indexOf('ACCEPTANCE_STATE:');
        if (at >= 0) { try { lastState = JSON.parse(text.slice(at + 17)); report.states.push(lastState); } catch {} }
      } else if (value.method === 'Runtime.exceptionThrown') report.errors.push(value.params.exceptionDetails);
    };
    const evaluate = async expression => {
      const result = await send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true, userGesture: true });
      if (result.exceptionDetails) throw Error(result.exceptionDetails.text);
      return result.result.value;
    };
    const probe = async (method, arg = '') => { await evaluate(`window.unityInstance.SendMessage('AcceptanceProbe',${JSON.stringify(method)},${JSON.stringify(arg)})`); await sleep(300); };
    const state = async () => { lastState = null; await probe('Snapshot'); for (let i = 0; i < 20 && !lastState; i++) await sleep(100); if (!lastState) throw Error('No probe state received.'); return lastState; };
    const capture = async label => {
      const name = `${width}x${height}-${label}.png`;
      const result = await send('Page.captureScreenshot', { format: 'png' });
      fs.writeFileSync(path.join(output, name), Buffer.from(result.data, 'base64'));
      report.captures.push(name); console.log('CAPTURE ' + name);
    };
    captureFailure = () => capture('failure');
    const viewport = async (w, h) => {
      width = w; height = h;
      await send('Emulation.setDeviceMetricsOverride', { width: w, height: h, deviceScaleFactor: 1, mobile: false });
      await evaluate(`document.body.style.margin='0';document.body.style.overflow='hidden';document.querySelector('#unity-container').style.cssText='position:fixed;inset:0;transform:none;width:100vw;height:100vh';document.querySelector('#unity-fullscreen-container').style.cssText='width:100vw;height:100vh';document.querySelector('canvas').style.cssText='display:block;width:100vw;height:100vh';document.querySelector('#unity-footer').style.display='none'`);
      await sleep(700);
    };
    const click = async (x, y) => {
      await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x, y });
      await send('Input.dispatchMouseEvent', { type: 'mousePressed', x, y, button: 'left', clickCount: 1 }); await sleep(90);
      await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x, y, button: 'left', clickCount: 1 }); await sleep(300);
    };
    const key = async (name, code, number) => {
      await send('Input.dispatchKeyEvent', { type: 'keyDown', key: name, code, windowsVirtualKeyCode: number }); await sleep(100);
      await send('Input.dispatchKeyEvent', { type: 'keyUp', key: name, code, windowsVirtualKeyCode: number }); await sleep(200);
    };
    const type = async text => { for (const digit of text) await key(digit, 'Digit' + digit, 48 + Number(digit)); };
    const enter = () => key('Enter', 'Enter', 13);
    const ready = async () => {
      for (let i = 0; i < 300; i++) { if (await evaluate('!!window.unityInstance')) return; await sleep(250); }
      throw Error('Unity did not load.');
    };
    await send('Runtime.enable'); await send('Page.enable'); await send('Page.bringToFront');
    await send('Emulation.setFocusEmulationEnabled', { enabled: true });
    await send('Page.navigate', { url: 'http://127.0.0.1:8878/' }); await ready(); await sleep(8000); await viewport(1280, 720);
    await capture('main-menu');
    // Start is an actual IMGUI mouse click, not a fixture action.
    await click(width / 2, (height - 650) / 2 + 54);
    check(!(await state()).menu, 'Main menu starts by mouse');

    if (!selected || selected === 'card') {
      await probe('Prepare', 'card'); let s = await state();
      check(s.stage === 'Payment' && s.sales === 0, 'Real card customer and complete basket');
      const opening = s.balance;
      await enter(); s = await state(); check(s.sales === 0 && s.message.includes('Hibás'), 'Empty terminal rejected through Enter');
      await type('123'); await enter(); s = await state(); check(s.balance === opening && s.sales === 0, 'Wrong terminal amount does not charge'); await capture('card-wrong');
      await key('Delete', 'Delete', 46); await type('22009'); await key('Backspace', 'Backspace', 8); s = await state(); check(s.entered === '2200', 'Terminal Backspace edits typed digits');
      await enter(); s = await state(); check(s.sales === 1 && s.balance === opening + 2200 && s.drawer === s.balance, 'Exact card input credits unified physical drawer');
      await enter(); check((await state()).sales === 1, 'Repeated Enter cannot double charge'); await capture('card-paid');
    }
    if (!selected || selected === 'triple') {
      await probe('Prepare', 'triple'); const s = await state();
      check(s.received === 3 && s.missing === 0 && s.stage === 'Payment', 'Three identical cigarettes recognized');
      check(s.receipts.every((p, i) => s.receipts.every((q, j) => i === j || Math.hypot(p.x - q.x, p.z - q.z) > .2)), 'Three identical items have separate positions'); await capture('triple');
    }
    if (!selected || selected === 'mouse') {
      await probe('Prepare', 'pickup'); await click(width / 2, height / 2); let s = await state();
      if (!s.held) { await click(width / 2, height / 2); s = await state(); }
      check(s.held === 'cig_red', 'Left click picks up physical cigarette'); await capture('mouse-held');
      await probe('Action', 'aim-place'); await click(width / 2, height / 2); s = await state();
      check(!s.held && s.received === 1, 'Left click places physical cigarette'); await capture('mouse-placed');
      await probe('Prepare', 'pickup'); await key('e', 'KeyE', 69); check((await state()).held === 'cig_red', 'Keyboard picks up physical cigarette');
      await probe('Action', 'aim-place'); await key('e', 'KeyE', 69); check(!(await state()).held, 'Keyboard places physical cigarette');
      await probe('Prepare', 'bat'); await click(width / 2, height / 2); s = await state(); check(s.tool === 'Bat' && s.message.includes('tolvajt'), 'Bat retains left-click swing action');
      await probe('Prepare', 'mop'); await send('Input.dispatchMouseEvent', { type: 'mousePressed', x: width / 2, y: height / 2, button: 'left', clickCount: 1 }); await sleep(2200);
      await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x: width / 2, y: height / 2, button: 'left', clickCount: 1 }); s = await state(); check(s.tool === 'Mop' && s.dirt === 0, 'Mop retains held left-click cleaning'); await capture('mop-cleaned');
    }
    if (!selected || selected === 'coffee') {
      await probe('Prepare', 'coffee');
      await key('3', 'Digit3', 51); let s = await state(); check(s.recipe === 11, 'Machine key selects latte');
      await key('e', 'KeyE', 69); s = await state(); check(s.coffee === 5 && s.message.includes('tejet'), 'Latte without milk preserves coffee'); await capture('coffee-no-milk');
      await probe('Action', 'milk'); await key('e', 'KeyE', 69); await probe('Action', 'finish-brew'); s = await state(); check(s.cup === 'latte' && s.milk === 1, 'Machine makes selected latte'); await capture('coffee-latte');
      await probe('Action', 'remove-cup'); await key('2', 'Digit2', 50); await key('e', 'KeyE', 69); await probe('Action', 'finish-brew'); check((await state()).cup === 'coffee_long', 'Machine makes long coffee');
      await probe('Action', 'remove-cup'); await key('1', 'Digit1', 49); await key('e', 'KeyE', 69); await probe('Action', 'finish-brew'); check((await state()).cup === 'coffee', 'Machine makes short coffee');
      await probe('Action', 'monitor-coffee'); await capture('coffee-monitor');
      await click(width * .5, height * 485 / 720); check((await state()).recipe === 10, 'Monitor mouse button selects long coffee');
      await click(width * .77, height * 485 / 720); check((await state()).recipe === 11, 'Monitor mouse button selects latte');
      await click(width * .23, height * 485 / 720); check((await state()).recipe === 9, 'Monitor mouse button selects short coffee');
    }
    if (!selected || selected === 'dialogue') {
      await probe('Prepare', 'dialogue'); let s = await state(); check(s.unavailable && s.modal === 'None', 'Real shortage offers inline dialogue without modal'); await capture('dialogue-shortage');
      await key('t', 'KeyT', 84); s = await state(); check(s.modal === 'None' && s.order.includes('Marlbroke') && s.sales === 0, 'Inline reply accepts available alternative without charging');
      await probe('Prepare', 'refuse'); await capture('dialogue-multi-item'); await key('t', 'KeyT', 84); s = await state(); check(s.missed === 1 && !s.order && s.sales === 0, 'Customer refuses substitution and leaves without charge');
    }
    if (!selected || selected === 'menu') {
      await probe('Prepare', 'pickup'); await probe('Action', 'live-clock'); await key('Escape', 'Escape', 27); let s = await state();
      check(s.menu && s.timeScale === 0 && s.blocked, 'Escape pauses world and input');
      const position = s.position, pausedElapsed = s.elapsed; await key('w', 'KeyW', 87); await sleep(500); s = await state(); check(Math.abs(position.z - s.position.z) < .01 && !s.held && s.elapsed === pausedElapsed, 'Pause blocks clock, movement and world interaction'); await capture('pause');
      await click(width / 2, (height - 650) / 2 + 160); await capture('settings');
      await click(width / 2, (height - 650) / 2 + 232); await key('r', 'KeyR', 82); check((await state()).interactionKey === 'R', 'Settings rebind interaction through actual GUI');
      await sleep(1500); await send('Page.reload'); await ready(); await sleep(8000); await viewport(1280, 720); check((await state()).interactionKey === 'R', 'Binding survives WebGL reload');
      await probe('Prepare', 'pickup'); await key('r', 'KeyR', 82); check((await state()).held === 'cig_red', 'Persisted remapped key operates real pickup');
      await probe('Prepare', 'menu');
      await click(width / 2, (height - 650) / 2 + 106); await capture('settings-persisted');
      await click(width / 2, (height - 650) / 2 + 590); check((await state()).interactionKey === 'E', 'Settings restore defaults through GUI');
      await viewport(1920, 1080); await capture('settings-large');
    }
    if (!selected || selected === 'arrangement') {
      await probe('Prepare', 'arrangement');
      let s = await state();
      const money = s.balance, item = s.stockedItem;
      await capture('arrangement-monitor');
      const openEditor = async () => {
        const current = await state();
        await click(current.arrangeButton.x, height - current.arrangeButton.y);
        check((await state()).arranging, 'TobaccOS mouse button enters arrangement');
      };
      const movePointer = async point => {
        await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: point.x, y: height - point.y });
        await sleep(350);
      };
      await openEditor();
      s = await state();
      check(s.timeScale === 0 && s.blocked && !s.menu, 'Arrangement pauses world and takes input without pause menu');
      await capture('arrangement-overview');
      await click(150, 130);
      check((await state()).selectedFurniture === 'public-shelf', 'Mouse selects movable shelf');
      await movePointer((await state()).badTarget);
      s = await state();
      check(!s.previewValid && s.dragging && s.shelfPosition.x > 5, 'Entrance obstruction shows invalid preview without moving shelf');
      await capture('arrangement-invalid');
      await movePointer(s.moveTarget);
      await key('e', 'KeyE', 69);
      s = await state();
      check(s.previewValid, 'Mouse position and keyboard rotation form a valid preview');
      await click(s.moveTarget.x, height - s.moveTarget.y);
      s = await state();
      check(!s.dragging && s.draftTurns === 1 && Math.abs(s.draftPosition.x - 3.75) < .01 && s.shelfPosition.x > 5 && s.stockedItem === item,
        'Placing preview changes draft only and keeps exact stock');
      await capture('arrangement-draft');
      await click(150, height - 38);
      s = await state();
      check(!s.arranging && s.shelfPosition.x > 5 && s.stockedItem === item && s.balance === money && s.timeScale === 1,
        'Cancel restores original world, inventory, cash and clock');
      await probe('Action', 'monitor-furnishing');
      await openEditor();
      await click(150, 130); await movePointer((await state()).moveTarget); await key('e', 'KeyE', 69);
      s = await state(); await click(s.moveTarget.x, height - s.moveTarget.y);
      await click(150, height - 86);
      s = await state();
      check(!s.arranging && s.shelfTurns === 1 && Math.abs(s.shelfPosition.x - 3.75) < .01 && s.stockedItem === item && s.balance === money && s.routeToShelf,
        'Save applies rotated stocked shelf with navigation and no charge');
      await capture('arrangement-saved');
      await probe('Action', 'monitor-furnishing'); await openEditor();
      await click(150, 178); await movePointer((await state()).fridgeTarget);
      s = await state(); check(s.selectedFurniture === 'drinks-fridge' && s.previewValid, 'Purchased fridge can also be moved');
      await click(s.fridgeTarget.x, height - s.fridgeTarget.y); await click(150, height - 86);
      s = await state(); check(!s.arranging && Math.abs(s.fridgePosition.x - 4.75) < .01 && Math.abs(s.fridgePosition.z - .75) < .01, 'Save retains fridge position');
      await probe('Action', 'monitor-furnishing'); await openEditor();
      await viewport(1920, 1080); await capture('arrangement-large'); await viewport(1280, 720);
      await click(150, height - 38);
      await probe('Action', 'finish-arranged-day'); await key('e', 'KeyE', 69); await enter();
      s = await state();
      check(s.day === 2 && s.phase === 'Preparing' && s.shelfTurns === 1 && Math.abs(s.shelfPosition.x - 3.75) < .01 && s.stockedItem === item && Math.abs(s.fridgePosition.x - 4.75) < .01,
        'Door exit and next day preserve saved layout and stocked item');
    }
    if (!selected || selected === 'entrance') {
      await probe('Prepare', 'entrance');
      let s = await state();
      check(s.phase === 'Preparing' && s.modal === 'None' && s.elapsed === 0, 'Preparation has no welcome modal and clock is stopped');
      await capture('entrance-closed');
      await key('e', 'KeyE', 69);
      await sleep(1500);
      s = await state();
      check(s.phase === 'Open' && s.elapsed > 0, 'E on sign opens shop and starts clock');
      let opened = s.entranceOpen;
      for (let i = 0; i < 40 && !opened; i++) { await sleep(200); opened = (await state()).entranceOpen; }
      check(opened, 'Arriving NPC opens physical entrance');
      await capture('entrance-npc-open');
      for (let i = 0; i < 40 && (await state()).entranceOpen; i++) await sleep(200);
      await probe('Action', 'aim-sign');
      await key('e', 'KeyE', 69);
      s = await state();
      check(s.phase === 'Closing' && s.modal === 'None', 'E on sign closes without summary');
      const stopped = s.elapsed;
      await probe('Action', 'aim-door');
      await key('e', 'KeyE', 69);
      check((await state()).phase === 'Closing', 'Door refuses end while customer is inside');
      await probe('Action', 'drain-shop');
      await sleep(2000);
      s = await state();
      check(s.phase === 'AfterHours' && s.canEndDay && s.modal === 'None' && s.elapsed === stopped && !s.entranceOpen, 'NPC leaves, door closes and player remains in closed shop');
      await probe('Action', 'view-entrance');
      await capture('entrance-after-hours');
      await viewport(1920, 1080);
      await capture('entrance-after-hours-large');
      await viewport(1280, 720);
      await send('Input.dispatchKeyEvent', { type: 'keyDown', key: 'w', code: 'KeyW', windowsVirtualKeyCode: 87 });
      await sleep(1000);
      await send('Input.dispatchKeyEvent', { type: 'keyUp', key: 'w', code: 'KeyW', windowsVirtualKeyCode: 87 });
      s = await state();
      check(s.position.z > -4.4 && s.phase === 'AfterHours', 'Player movement cannot cross entrance or silently end day');
      await key('e', 'KeyE', 69);
      s = await state();
      check(s.phase === 'Summary', 'E at closed entrance ends day');
      await capture('entrance-summary');
      await enter();
      s = await state();
      check(s.day === 2 && s.phase === 'Preparing' && s.modal === 'None' && s.elapsed === 0, 'Next day has no welcome modal and awaits sign');
      await capture('entrance-next-day');
    }
    if (!selected || selected === 'summary') {
      await probe('Prepare', 'summary'); let s = await state(); check(s.phase === 'AfterHours' && s.modal === 'None', 'Closed shop stays playable'); await key('e', 'KeyE', 69); s = await state(); check(s.phase === 'Summary', 'Door interaction ends day'); await capture('summary');
      await enter(); s = await state(); check(s.day === 2 && s.phase === 'Preparing' && s.balance === s.drawer, 'Enter starts next day preserving drawer'); await capture('next-day');
    }
    report.result = 'passed';
  } catch (error) { report.result = 'failed'; report.failure = error.stack ?? String(error); console.error(report.failure); try { await captureFailure?.(); } catch {} process.exitCode = 1; }
  finally {
    report.errors.push(...messages.filter(m => m.type === 'error'));
    if (report.errors.length) { report.result = 'failed'; report.failure ??= 'Browser console errors.'; process.exitCode = 1; }
    report.finished = new Date().toISOString();
    fs.writeFileSync(path.join(output, 'report.json'), JSON.stringify(report, null, 2));
    fs.writeFileSync(path.join(output, 'console.json'), JSON.stringify(messages, null, 2));
    console.log('REPORT ' + output); ws?.close(); browser?.kill(); server?.close();
  }
})();

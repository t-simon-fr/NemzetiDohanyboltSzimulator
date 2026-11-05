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
    const clickRect = rect => click(rect.x + rect.width / 2, rect.y + rect.height / 2);
    const key = async (name, code, number) => {
      await send('Input.dispatchKeyEvent', { type: 'keyDown', key: name, code, windowsVirtualKeyCode: number }); await sleep(100);
      await send('Input.dispatchKeyEvent', { type: 'keyUp', key: name, code, windowsVirtualKeyCode: number }); await sleep(200);
    };
    const hold = async (keys, milliseconds) => {
      let modifiers = 0;
      for (const [key, code, windowsVirtualKeyCode] of keys) {
        if (code === 'ShiftLeft' || code === 'ShiftRight') modifiers |= 8;
        await send('Input.dispatchKeyEvent', { type: 'keyDown', key, code, windowsVirtualKeyCode, modifiers });
      }
      await sleep(milliseconds);
      for (const [key, code, windowsVirtualKeyCode] of [...keys].reverse()) {
        if (code === 'ShiftLeft' || code === 'ShiftRight') modifiers &= ~8;
        await send('Input.dispatchKeyEvent', { type: 'keyUp', key, code, windowsVirtualKeyCode, modifiers });
      }
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
    await clickRect((await state()).menuStart);
    check(!(await state()).menu, 'Main menu starts by mouse');

    if (!selected || selected === 'cash-register') {
      const amount = text => Number(text.replace(/[^0-9]/g, ''));
      const normalFov = (await state()).cameraFov;
      await probe('Prepare', 'cash'); await sleep(500); let s = await state();
      const balance = s.balance, price = s.cashPrice;
      check(s.device === 'Till' && s.cameraFov === 42 && s.cameraFov < normalFov, 'Cash checkout opens dedicated close lens');
      check(amount(s.cashDue) === price && amount(s.cashReceived) === s.cashTender && amount(s.cashChange) === s.cashTender - price, 'Own register display shows real tender and required change');
      check(s.cashKeys.every(r => r.width > 20 && r.x >= 0 && r.y >= 0 && r.x + r.width <= width && r.y + r.height < height - 48), 'Physical cash commands remain visible and clickable');
      await capture('cash-register-ready');
      await clickRect(s.cashKeys[3]); s = await state();
      check(s.sales === 0 && s.balance === balance, 'Physical pay rejects empty change without charging');
      await clickRect(s.cashDenominations[5]); s = await state();
      check(amount(s.cashSelected) === 500 && s.balance === balance, 'Physical note selection updates own screen without charging');
      await clickRect(s.cashKeys[0]); s = await state();
      check(amount(s.cashSelected) === 0, 'Physical undo updates screen');
      await clickRect(s.cashDenominations[5]); s = await state(); await clickRect(s.cashKeys[1]); s = await state();
      check(amount(s.cashSelected) === 0, 'Physical clear resets selected change');
      await key('4', 'Digit4', 52); s = await state();
      check(amount(s.cashSelected) === 2000, 'Keyboard shares cash screen state');
      for (const index of [5, 6, 7]) { await clickRect(s.cashDenominations[index]); s = await state(); }
      check(amount(s.cashSelected) === s.cashTender - price && s.balance === balance, 'Actual note and coin clicks produce exact unpaid change');
      await capture('cash-register-selected');
      await viewport(1920, 1080); s = await state(); await capture('cash-register-selected-large');
      check(s.cashKeys.every(r => r.width > 20 && r.y + r.height < height - 48), 'Large cash view retains all commands above help bar');
      await clickRect(s.cashKeys[3]); s = await state();
      check(s.sales === 1 && s.balance === balance + price && s.drawer === s.balance, 'Physical cash approval settles exactly once');
      check(s.device === 'None' && s.cameraFov === normalFov, 'Completed cash sale restores normal camera');
      await enter(); s = await state();
      check(s.sales === 1 && s.balance === balance + price, 'Repeated Enter cannot charge completed cash sale twice');
      await viewport(1280, 720); await probe('Prepare', 'cash'); await probe('Action', 'live-clock'); await key('Escape', 'Escape', 27); s = await state();
      check(s.device === 'None' && !s.menu && s.cameraFov === normalFov, 'Escape leaves register and restores original lens');
    }

    if (!selected || selected === 'hand-interaction') {
      await probe('Prepare', 'hand-interaction'); let s = await state();
      const balance = s.balance, normalFov = s.cameraFov;
      check(s.device === 'None' && s.lookingAtHand && s.handPrompt.includes('Aprópénz'), 'Normal view targets the actual offered palm with a useful prompt');
      await capture('hand-world-prompt');
      await key('e', 'KeyE', 69); s = await state();
      check(s.device === 'Coins' && s.coinSelected === 0, 'E on the hand opens coin view without selecting money');
      check(s.handUpright && s.handSide > .9 && s.cameraFov === 46, 'Close view stays upright on the player side of the customer hand');
      await capture('hand-player-view');
      await probe('Action', 'live-clock'); await key('Escape', 'Escape', 27); s = await state();
      check(s.device === 'None' && s.cameraFov === normalFov && s.balance === balance, 'Escape restores ordinary player view without charging');
      await probe('Action', 'aim-customer-hand'); s = await state();
      await send('Input.dispatchMouseEvent', {type:'mousePressed',x:width/2,y:height/2,button:'left',clickCount:1}); await sleep(90);
      await send('Input.dispatchMouseEvent', {type:'mouseReleased',x:width/2,y:height/2,button:'left',clickCount:1}); await sleep(300); s = await state();
      check(s.device === 'Coins' && s.coinSelected === 0 && s.sales === 0, 'Actual click on the hand opens once and cannot leak into coin selection');
      const value = s.offeredCoins[0]; await clickRect(s.coinTargets[0]); s = await state();
      check(s.coinSelected === value && s.balance === balance, 'Coin clicks still select in the corrected view');
      await clickRect(s.coinClear); s = await state();
      check(s.coinSelected === 0, 'Clear retains the corrected readable view');
      await viewport(1920,1080); s = await state(); await capture('hand-player-view-large');
      check(s.coinTargets.every(r => r.width > 20 && r.y > 65 && r.y+r.height < height-170), 'All coin targets stay above controls in large player view');
      await viewport(1280,720);
    }

    if (!selected || selected === 'forint-coins') {
      await probe('Prepare', 'coins'); let s = await state();
      const balance = s.balance, price = s.cashPrice;
      check(s.device === 'Coins' && new Set(s.offeredCoins).size === 6, 'Palm presents all six official coin denominations');
      check(s.coinTargets.every(r => r.width > 20 && r.height > 12 && r.x > 0 && r.y > 0 && r.x + r.width < width && r.y + r.height < height - 170), 'Every offered coin has an unobstructed on-screen target');
      await capture('forint-coins-ready');
      await clickRect(s.coinPay); s = await state();
      check(s.sales === 0 && s.balance === balance, 'Empty palm approval cannot charge');
      const firstValue = s.offeredCoins[0];
      await clickRect(s.coinTargets[0]); s = await state();
      check(s.coinSelected === firstValue && s.selectedCoins[0], 'Actual coin face click selects its denomination');
      await clickRect(s.coinTargets[0]); s = await state();
      check(s.coinSelected === 0 && !s.selectedCoins[0], 'Actual second coin click deselects');
      await clickRect(s.coinTargets[0]); s = await state(); await clickRect(s.coinClear); s = await state();
      check(s.coinSelected === 0 && s.balance === balance, 'Clear returns selected coins without charging');
      for (const value of [200, 100, 50, 20, 10, 5]) {
        const index = s.offeredCoins.findIndex((v, i) => v === value && !s.selectedCoins[i]);
        await clickRect(s.coinTargets[index]); s = await state();
      }
      check(s.coinSelected === price && s.sales === 0 && s.balance === balance, 'Actual clicks on all six denominations select exact unpaid amount');
      await capture('forint-coins-selected');
      await viewport(1920, 1080); s = await state(); await capture('forint-coins-selected-large');
      check(s.coinTargets.every(r => r.width > 20 && r.y + r.height < height - 170), 'Large palm view keeps coins above controls');
      await clickRect(s.coinPay); s = await state();
      check(s.sales === 1 && s.balance === balance + price && s.drawer === s.balance, 'Real coin approval settles once with conserved cash');
      check(s.device === 'None' && s.offeredCoins.length === 0, 'Accepted payment clears the physical palm and leaves close view');
      await enter(); s = await state();
      check(s.sales === 1 && s.balance === balance + price, 'Repeated approval cannot charge coin sale twice');
      await viewport(1280, 720);
    }

    if (!selected || selected === 'warehouse-storage') {
      // Pointer-locked world clicks must not move the cursor and rotate the camera.
      const worldClick = async () => {
        await send('Input.dispatchMouseEvent', { type:'mousePressed', x:width/2, y:height/2, button:'left', clickCount:1 }); await sleep(100);
        await send('Input.dispatchMouseEvent', { type:'mouseReleased', x:width/2, y:height/2, button:'left', clickCount:1 }); await sleep(300);
      };
      await probe('Prepare', 'warehouse-storage'); let s = await state();
      const cash = s.balance;
      check(s.storagePlaces > 9 && s.storageTypes[0] === 0 && s.storageTypes[1] === 1, 'Real shipments occupy physical storage; previously unassigned rows are usable');
      await key('e', 'KeyE', 69); s = await state();
      check(s.heldCartonType === 0 && s.cartonQuantity === 2 && !s.storageLabels[0], 'Actual E picks carton and clears vacated source');
      await probe('Action', 'aim-storage-2'); await key('e', 'KeyE', 69); s = await state();
      check(s.heldCartonType === -1 && s.storageTypes[2] === 0 && s.storageLabels[2] === 'Lucky Stroke', 'Actual E places carton on another former brand dock');
      await capture('warehouse-relocated');
      await probe('Action', 'aim-storage-0'); await key('e', 'KeyE', 69); s = await state();
      check(s.heldCartonType === -1 && s.storageTypes[2] === 0, 'Vacant source cannot remotely pick up relocated carton');
      await probe('Action', 'aim-storage-2'); await worldClick(); s = await state();
      check(s.heldCartonType === 0 && s.cartonQuantity === 2 && !s.storageLabels[2], 'Actual mouse click picks the physically relocated carton');
      await probe('Action', 'aim-storage-1'); await key('e', 'KeyE', 69); s = await state();
      check(s.heldCartonType === 0 && s.storageTypes[1] === 1, 'Occupied storage rejects E without replacing its different-brand carton');
      await probe('Action', 'aim-storage-lower'); await key('e', 'KeyE', 69); s = await state();
      check(s.heldCartonType === -1 && s.lowerStorageLabel === 'Lucky Stroke', 'Actual E places carton on formerly unusable lower shelf row');
      await viewport(1920,1080); await capture('warehouse-lower-relocated');
      await probe('Action', 'aim-storage-lower'); await worldClick(); s = await state();
      check(s.heldCartonType === 0 && !s.lowerStorageLabel, 'Mouse can retrieve carton from lower shelf row');
      await probe('Action', 'aim-stock-shelf'); await key('e', 'KeyE', 69); s = await state();
      check(s.cartonQuantity === 1 && s.warehouseGoods === 1 && s.retailGoods === 1, 'Relocated carton still unpacks exactly one finite product');
      await probe('Action', 'aim-storage-2'); await worldClick(); s = await state();
      check(s.heldCartonType === -1 && s.storageTypes[2] === 0, 'Mouse places part-filled carton at chosen empty warehouse dock');
      await probe('Action', 'aim-stock-shelf'); await key('e', 'KeyE', 69); s = await state();
      check(s.held === 'cig_red', 'Actual E picks the stocked unit for carton return');
      await probe('Action', 'aim-storage-2'); await key('e', 'KeyE', 69); s = await state();
      check(!s.held && s.warehouseGoods === 2 && s.retailGoods === 0 && s.storageTypes[2] === 0, 'Unit return uses actual carton contents instead of dock original brand');
      check(s.balance === cash && s.storageTypes[1] === 1, 'All keyboard and mouse relocation preserves exact cash and other carton');
      await capture('warehouse-returned-large');
      await viewport(1280,720); await capture('warehouse-returned');
    }

    if (!selected || selected === 'shelf-labels') {
      await probe('Prepare', 'shelf-labels'); let s = await state();
      check(!s.warehouseShelfLabel && !s.retailShelfLabel && !s.supplyPrompt, 'Empty shelves have blank labels and no pickup prompt');
      await capture('shelf-warehouse-empty');
      const startCash = s.balance;
      await probe('Action', 'order-shelf-delivery'); s = await state();
      check(!s.warehouseShelfLabel, 'Pending delivery leaves empty shelf label blank');
      for (let i = 0; i < 40 && !(await state()).warehouseShelfLabel; i++) await sleep(200);
      s = await state(); const paidCash = s.balance;
      check(s.warehouseShelfLabel === 'Lucky Stroke' && s.warehouseGoods === 2 && paidCash < startCash, 'Real arrival labels delivered goods and charges purchase once');
      await capture('shelf-warehouse-delivered');
      await key('e', 'KeyE', 69); s = await state();
      check(s.cartonQuantity === 2 && !s.warehouseShelfLabel && s.warehouseGoods === 2, 'Actual E carries carton and clears vacated shelf label');
      await probe('Action', 'aim-stock-shelf'); await key('e', 'KeyE', 69); s = await state();
      check(s.retailShelfLabel.includes('Lucky Stroke') && s.retailShelfLabel.includes('Ft') && s.cartonQuantity === 1, 'Actual E stocks product and updates retail name and price');
      check(s.warehouseGoods === 1 && s.retailGoods === 1 && s.balance === paidCash, 'Manual stocking preserves goods and money');
      await probe('Action', 'aim-warehouse-shelf'); await key('e', 'KeyE', 69); s = await state();
      check(s.cartonQuantity === -1 && s.warehouseShelfLabel === 'Lucky Stroke', 'Actual E returns remaining carton and restores warehouse label');
      await viewport(1920, 1080); await capture('shelf-warehouse-returned-large');
      await probe('Action', 'aim-stock-shelf'); await key('e', 'KeyE', 69); s = await state();
      check(s.held === 'cig_red' && !s.retailShelfLabel, 'Taking last retail product clears label while source remains safely reserved');
      await key('e', 'KeyE', 69); s = await state();
      check(!s.held && s.retailShelfLabel.includes('Lucky Stroke'), 'Replacing product restores label through actual input');
      await capture('shelf-retail-stocked-large');
      await viewport(1280, 720); await capture('shelf-retail-stocked');
      check(s.balance === paidCash && s.warehouseGoods + s.retailGoods === 2, 'Label and pickup operations preserve final money and total goods');
    }

    if (!selected || selected === 'movement') {
      const forward = ['w', 'KeyW', 87], sprint = ['Shift', 'ShiftLeft', 16], jump = [' ', 'Space', 32];
      await probe('Prepare', 'movement'); await sleep(300); let start = await state();
      await hold([forward], 500); let s = await state(); const walked = s.position.z - start.position.z;
      check(walked > 1.3 && walked < 2.2, 'Actual W input walks at normal speed');
      await probe('Prepare', 'movement'); await sleep(300); start = await state();
      await hold([sprint, forward], 500); s = await state();
      check(s.position.z - start.position.z > walked * 1.35, 'Actual Shift+W input sprints faster');
      await probe('Prepare', 'movement'); await sleep(300); start = await state();
      await send('Input.dispatchKeyEvent', { type: 'keyDown', key: ' ', code: 'Space', windowsVirtualKeyCode: 32 });
      await sleep(100); s = await state();
      check(s.position.y > start.position.y + .2 && !s.grounded, 'Actual Space input jumps from ground');
      await sleep(700); s = await state();
      check(s.grounded && Math.abs(s.position.y - start.position.y) < .08, 'Holding Space lands without automatic repeated jumps');
      await send('Input.dispatchKeyEvent', { type: 'keyUp', key: ' ', code: 'Space', windowsVirtualKeyCode: 32 });
      await key('Escape', 'Escape', 27); start = await state(); await hold([sprint, forward, jump], 400); s = await state();
      check(s.menu && s.position.y === start.position.y && s.position.z === start.position.z, 'Pause blocks actual sprint and jump inputs');
      await probe('Prepare', 'summary'); await hold([sprint, forward, jump], 900); s = await state();
      check(s.position.z > -4.4 && s.phase === 'AfterHours', 'Sprint and jump cannot cross real shop entrance boundary');
    }
    if (!selected || selected === 'physical-terminal') {
      await probe('Prepare', 'card'); let s = await state(); const balance = s.balance;
      check(s.device === 'Terminal', 'Card checkout focuses actual terminal');
      for (let digit = 0; digit < 10; digit++) {
        await clickRect((await state()).cardClear);
        await clickRect((await state()).cardDigits[digit]);
        check((await state()).entered === String(digit), 'Actual physical numeric button ' + digit);
      }
      await clickRect((await state()).cardClear);
      for (const digit of [1, 2, 3]) await clickRect((await state()).cardDigits[digit]);
      await clickRect((await state()).cardConfirm); s = await state();
      check(s.balance === balance && s.sales === 0 && s.cardDisplayStatus === 'HIBÁS ÖSSZEG', 'Physical terminal rejects wrong input on its own display');
      await capture('physical-terminal-error');
      await clickRect(s.cardUndo); await type('4'); s = await state();
      check(s.entered === '124' && s.cardDisplayAmount === '124 Ft', 'Physical correction and keyboard share live terminal amount');
      await clickRect(s.cardClear);
      for (const digit of [2, 2, 0, 0]) await clickRect((await state()).cardDigits[digit]);
      await clickRect((await state()).cardConfirm); s = await state();
      check(s.sales === 1 && s.balance === balance + 2200 && s.cardDisplayStatus === 'SIKERES FIZETÉS', 'Physical OK pays once and keeps success visible');
      await capture('physical-terminal-approved');
      await clickRect(s.cardConfirm); check((await state()).sales === 1, 'Repeated physical OK cannot double charge');
      await viewport(1920, 1080); await probe('Prepare', 'card');
      await capture('physical-terminal-large');
      await clickRect((await state()).cardBack); check((await state()).device === 'None', 'Physical ESC leaves terminal at large viewport');
      await viewport(1280, 720);
    }
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
    if (!selected || selected === 'coffee-display') {
      await viewport(1280, 720);
      await probe('Prepare', 'coffee-display'); await key('e', 'KeyE', 69); let s = await state();
      check(s.device === 'Coffee' && s.coffeeScreenStatus === 'KÉSZENLÉT', 'Actual E opens own coffee machine display');
      await capture('coffee-display-ready');
      const balance = s.balance;
      await clickRect(s.coffeeButtons[2]); s = await state();
      check(s.recipe === 11 && s.coffeeScreenRecipe === 'Latte', 'Physical touchscreen selects latte');
      await clickRect(s.coffeeButtons[3]); s = await state();
      check(s.brewRemaining > 0 && s.coffee === 4 && s.milk === 1 && s.coffeeScreenStatus.startsWith('FŐZÉS'), 'Machine starts real brew and displays ingredient/progress state');
      await capture('coffee-display-brewing');
      await clickRect(s.coffeeButtons[0]); s = await state();
      check(s.recipe === 11 && s.coffee === 4 && s.milk === 1, 'Busy physical recipe key cannot change drink or consume twice');
      await key('Escape', 'Escape', 27); s = await state();
      check(s.device === 'None' && !s.menu, 'Escape exits machine without opening pause menu');
      await key('Escape', 'Escape', 27); s = await state(); const paused = s.brewRemaining;
      await sleep(600); s = await state();
      check(s.menu && s.brewRemaining === paused, 'Pause freezes real ongoing brew');
      await key('Escape', 'Escape', 27);
      s = await state();
      check(!s.menu && !s.blocked && s.timeScale === 1, 'Resuming pause restores active world context');
      for (let i = 0; i < 30 && (await state()).cup !== 'latte'; i++) await sleep(100);
      await probe('Action', 'aim-coffee-display'); await key('e', 'KeyE', 69); s = await state();
      check(s.cup === 'latte' && s.coffeeScreenStatus.startsWith('ELKÉSZÜLT'), 'Machine finishes after leaving view and reports actual ready latte');
      await capture('coffee-display-latte');
      await clickRect(s.coffeeButtons[3]); s = await state();
      check(s.held === 'latte' && s.device === 'None' && s.coffeeSlotHeld, 'Physical take gives actual cup without same-click replacement');
      await probe('Action', 'remove-cup');
      for (const [index, type] of [[0, 'coffee'], [1, 'coffee_long']]) {
        await probe('Action', 'aim-coffee-display'); await key('e', 'KeyE', 69); s = await state();
        await clickRect(s.coffeeButtons[index]); await clickRect((await state()).coffeeButtons[3]);
        for (let i = 0; i < 30 && (await state()).cup !== type; i++) await sleep(100);
        s = await state(); check(s.cup === type && s.coffeeScreenStatus.startsWith('ELKÉSZÜLT'), 'Physical touchscreen brews actual ' + type);
        await clickRect(s.coffeeButtons[3]); check((await state()).held === type, 'Physical take collects ' + type);
        await probe('Action', 'remove-cup');
      }
      await viewport(1920, 1080);
      await probe('Action', 'aim-coffee-display'); await key('e', 'KeyE', 69); s = await state();
      check(s.coffee === 2 && s.milk === 1 && s.balance === balance, 'Three physical coffee recipes preserve exact stock and money');
      await probe('Action', 'empty-coffee-milk'); await clickRect((await state()).coffeeButtons[2]); s = await state();
      check(s.coffeeScreenStatus === 'NINCS TEJ' && !s.coffeeActionEnabled, 'Machine display explains latte milk shortage');
      await clickRect(s.coffeeButtons[3]); s = await state();
      check(s.coffee === 2 && s.brewRemaining === 0, 'Disabled physical brew does not consume stock');
      await capture('coffee-display-no-milk-large');
      await probe('Action', 'empty-coffee-supplies'); s = await state();
      check(s.coffeeScreenStatus === 'NINCS KÁVÉALAPANYAG' && !s.coffeeActionEnabled, 'Machine display continuously reports coffee shortage');
      await capture('coffee-display-no-coffee-large');
      await key('1', 'Digit1', 49); s = await state();
      check(s.recipe === 9 && s.coffeeScreenRecipe === 'Rövid kávé', 'Machine keyboard and touchscreen use same recipe');
      await key('Escape', 'Escape', 27); await viewport(1280, 720);
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
      await clickRect((await state()).menuSettings); await capture('settings');
      await clickRect((await state()).menuBinding); await key('r', 'KeyR', 82); check((await state()).interactionKey === 'R', 'Settings rebind interaction through actual GUI');
      await sleep(1500); await send('Page.reload'); await ready(); await sleep(8000); await viewport(1280, 720); check((await state()).interactionKey === 'R', 'Binding survives WebGL reload');
      await probe('Prepare', 'pickup'); await key('r', 'KeyR', 82); check((await state()).held === 'cig_red', 'Persisted remapped key operates real pickup');
      await probe('Prepare', 'menu');
      await clickRect((await state()).menuSettings); await capture('settings-persisted');
      await clickRect((await state()).menuDefaults); check((await state()).interactionKey === 'E', 'Settings restore defaults through GUI');
      await viewport(1920, 1080); await capture('settings-large');
    }
    if (!selected || selected === 'ui') {
      await viewport(1280, 720);
      await probe('Prepare', 'ui-order'); let s = await state();
      const balance = s.balance, elapsed = s.elapsed;
      check(s.orderLines.length === 2 && s.orderLines[0].Required === 2 && s.orderLines[0].Delivered === 0 && s.modal === 'None', 'Customer panel groups duplicate order without pausing');
      await capture('ui-order');
      await probe('Action', 'deliver-ui-one'); s = await state();
      check(s.orderLines[0].Delivered === 1 && s.orderLines[0].Required === 2 && s.balance === balance, 'Customer panel tracks actual partial handover without charging');
      await probe('Action', 'ui-long-dialogue'); await capture('ui-long-dialogue');
      await sleep(6500); s = await state();
      check(!s.uiNotice && s.orderLines[0].Delivered === 1 && s.spoken.includes('szomszéd'), 'Transient notice expires while dialogue and order remain');
      check(s.elapsed === elapsed && s.balance === balance && s.modal === 'None', 'Presentation does not advance manual fixture or mutate money');
      await viewport(1920, 1080); await capture('ui-long-dialogue-large');
      await probe('Prepare', 'card'); await type('123');
      await clickRect((await state()).cardConfirm); s = await state();
      check(s.sales === 0 && s.message.includes('Hibás'), 'Restyled terminal mouse button rejects wrong amount');
      await capture('ui-card-error-large');
      await key('Delete', 'Delete', 46); await type('2200'); await clickRect((await state()).cardConfirm);
      s = await state();
      check(s.sales === 1 && s.orderLines.every(line => line.Delivered === line.Required), 'Restyled terminal settles once and keeps paid order complete');
      await probe('Prepare', 'summary'); await key('e', 'KeyE', 69); await capture('ui-receipt-large');
      await viewport(1280, 720); await capture('ui-receipt');
      // Virtual summary coordinates map through the same 1280x720 canvas.
      await click(640, 574); s = await state();
      check(s.day === 2 && s.phase === 'Preparing' && s.modal === 'None', 'Fixed receipt action starts next day by mouse');
      await key('w', 'KeyW', 87); check((await state()).movementLearned, 'Movement dismisses one-time navigation hint');
      await viewport(1920, 1080); await probe('Prepare', 'menu'); await capture('ui-menu-large');
      await clickRect((await state()).menuSettings); await capture('ui-settings-large');
      await clickRect((await state()).menuBack); await clickRect((await state()).menuStart);
      check(!(await state()).menu, 'Settings back and menu resume work by mouse at large viewport');
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
    if (!selected || selected === 'door-storage') {
      await probe('Prepare', 'counter-storage');
      let s = await state();
      check(s.privateStock === 5 && !s.held, 'Five physical Dnyipro packs fit under counter');
      await capture('private-stock');
      await key('e', 'KeyE', 69); s = await state();
      // Pickup reserves its source slot until placement, allowing safe cancellation.
      check(s.held === 'dnyipro' && s.privateStock === 5, 'E retrieves real private pack and reserves its source slot');
      await key('e', 'KeyE', 69); s = await state();
      check(!s.held && s.privateStock === 5, 'E returns pack to dedicated under-counter slot');
      await probe('Action', 'view-door-outside'); await sleep(700);
      s = await state(); check(s.outwardSign === 'ZÁRVA', 'Closed sign reads ZARVA from street');
      await capture('door-outside-closed');
      await key('e', 'KeyE', 69); await sleep(700); s = await state();
      check(s.phase === 'Open' && s.outwardSign === 'NYITVA', 'E flips door sign to street-facing NYITVA');
      await capture('door-outside-open');
      await key('e', 'KeyE', 69); await sleep(700); s = await state();
      check(s.phase === 'Closing' && s.outwardSign === 'ZÁRVA', 'E flips back without ending day');
      await probe('Action', 'view-door-inside'); await capture('door-inside');
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
      // Walking closer changes the ray height at the door; aim below the sign again.
      await probe('Action', 'aim-door');
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

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
    let snapshotParts=[];
    ws.onmessage = event => {
      const value = JSON.parse(event.data);
      if (value.id) {
        const p = pending.get(value.id); if (!p) return;
        clearTimeout(p.timer); pending.delete(value.id); value.error ? p.reject(Error(JSON.stringify(value.error))) : p.resolve(value.result);
      } else if (value.method === 'Runtime.consoleAPICalled') {
        const text = value.params.args.map(a => a.value ?? a.description ?? '').join(' ');
        messages.push({ type: value.params.type, text });
        const chunk=text.match(/^ACCEPTANCE_CHUNK:(\d+)\/(\d+):([\s\S]*)/);
        if(chunk){
          const index=Number(chunk[1]),count=Number(chunk[2]);if(index===0)snapshotParts=[];
          snapshotParts[index]=chunk[3].replace(/\n$/,'');
          if(index+1===count&&snapshotParts.filter(p=>p!==undefined).length===count){
            try{lastState=JSON.parse(snapshotParts.join(''));report.states.push(lastState);}catch{}
          }
        }
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

    if (!selected || selected === 'ingredient-pickup' || selected === 'closing-fixes') {
      const worldClick=async()=>{
        await send('Input.dispatchMouseEvent',{type:'mousePressed',x:width/2,y:height/2,button:'left',clickCount:1});await sleep(100);
        await send('Input.dispatchMouseEvent',{type:'mouseReleased',x:width/2,y:height/2,button:'left',clickCount:1});await sleep(300);
      };
      await probe('Prepare','ingredient-pickup');let s=await state();const cash=s.balance;
      check(s.ingredientTarget==='coffee'&&s.readyCoffee===10&&s.readyMilk===10,'Real delivered coffee package is targetable through its empty storage-cell trigger');
      await capture('ingredient-coffee-target');
      await key('e','KeyE',69);s=await state();
      check(s.tool==='CoffeeRefill'&&s.readyCoffee===10,'Actual E picks the paid coffee ingredient package');
      await probe('Action','aim-refill-machine');await key('e','KeyE',69);s=await state();
      check(!s.tool&&s.ingredientCoffee===10&&s.readyCoffee===0,'Actual E refills all ten coffee servings exactly once');
      await probe('Action','aim-ingredient-milk');s=await state();
      check(s.ingredientTarget==='milk','Milk package is targetable instead of the blank warehouse shelf');
      await capture('ingredient-milk-target');
      await worldClick();s=await state();
      check(s.tool==='CoffeeRefill'&&s.readyMilk===10&&s.readyCoffee===0,'Actual mouse click picks the physically targeted milk package');
      await probe('Action','aim-refill-machine');await key('e','KeyE',69);s=await state();
      check(!s.tool&&s.ingredientMilk===10&&s.readyMilk===0&&s.ingredientCoffee===10,'Actual E refills milk without extra coffee brewing or duplicated ingredients');
      check(s.balance===cash,'Ingredient pickup and refill never charge or lose money');
      await probe('Action','view-refilled-machine');await viewport(1920,1080);await capture('ingredient-machine-refilled-large');await viewport(1280,720);
    }

    if (!selected || selected === 'after-hours-upgrades' || selected === 'closing-fixes') {
      await probe('Prepare','after-hours-upgrades');let s=await state();
      check(s.phase==='AfterHours'&&s.canDevelop&&s.device==='Monitor','Closed empty shop opens normal development app after hours');
      await capture('after-hours-upgrades-before');
      for(let i=0;i<5;i++){
        const before=s.balance,point=s.upgradeButtons[i];
        await click(point.x,height-point.y);s=await state();
        check(s.upgradesOwned[i]&&s.balance===before-s.upgradeCosts[i],'Real computer click buys after-hours development exactly once: '+i);
      }
      const balance=s.balance,p=s.upgradeButtons[4];await click(p.x,height-p.y);s=await state();
      check(s.balance===balance&&s.upgradesOwned.every(Boolean),'Owned development cannot be charged again after hours');
      await viewport(1920,1080);await capture('after-hours-upgrades-owned-large');await viewport(1280,720);
    }

    if (!selected || selected === 'stock-orders') {
      await probe('Prepare','stock-orders'); let s=await state();
      const cash=s.balance;
      check(s.orderLines.length===1 && s.orderLines[0].Type===6 && s.orderLines[0].Required===1, 'Actual customer requests only the energy drink genuinely purchased into the otherwise empty shop');
      check(!s.unavailable && s.stage==='Order', 'Warehouse-only goods are requestable without a false shortage');
      await capture('stock-order-warehouse');
      await probe('Action','take-stock-order'); s=await state();
      check(s.held==='energy' && s.balance===cash, 'Take the actual paid energy drink for the generated request');
      await key('e','KeyE',69); s=await state();
      check(!s.held && s.stage==='Payment' && s.received===1, 'Actual E delivery satisfies the stock-selected customer order');
      check(s.orderLines[0].Delivered===1 && s.balance===cash, 'Order UI follows real delivery without booking an unpaid sale');
      await capture('stock-order-delivered');
      await viewport(1920,1080); await capture('stock-order-delivered-large');
      await viewport(1280,720);
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
    if (!selected || selected === 'physical-terminal' || selected === 'closing-fixes') {
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
      check(s.device === 'None' && !s.blocked && s.cursorLocked, 'Successful card payment automatically restores normal player view and input');
      await capture('physical-terminal-approved');
      await enter(); check((await state()).sales === 1, 'Repeated confirmation after auto-close cannot double charge');
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
    if (selected === 'visual-refresh') {
      const control=async id=>{const s=await state();const i=s.computerControlIds.indexOf(id);if(i<0)throw Error('Missing computer target '+id);return s.computerControls[i];};
      const press=async id=>clickRect(await control(id));
      const fill=async(rect,text)=>{
        await clickRect(rect);
        await send('Input.dispatchKeyEvent',{type:'keyDown',key:'a',code:'KeyA',windowsVirtualKeyCode:65,modifiers:2});
        await send('Input.dispatchKeyEvent',{type:'keyUp',key:'a',code:'KeyA',windowsVirtualKeyCode:65,modifiers:2});
        await key('Backspace','Backspace',8);
        for(const character of String(text))await send('Input.dispatchKeyEvent',{type:'char',text:character,unmodifiedText:character});await sleep(300);
      };
      await probe('Prepare','visual-refresh');let s=await state();
      await capture('desktop');
      await press('OpenApp:1');s=await state();const cash=s.balance;
      await fill(s.cartFields[0],'2');s=await state();
      check(s.cartQuantities[0]===2&&s.balance===cash,'Typed order quantity stays a draft until purchase');
      await clickRect(s.cartSubmit);s=await state();
      check(s.pendingCart[0]===2&&s.balance===cash-3000&&s.drawer===s.balance,'New order screen buys exact typed cart once');await capture('orders');
      await press('Desktop:0');await press('OpenApp:2');s=await state();const funds=s.balance;
      await press('Expand:0');s=await state();check(s.upgradesOwned[0]&&s.balance===funds-24000,'New upgrade list buys real shop expansion');
      await press('CoffeeMachineUpgrade:0');s=await state();check(s.upgradesOwned[2]&&s.balance===funds-40000,'New upgrade list buys physical coffee machine');await capture('upgrades');
      await press('Desktop:0');await press('OpenApp:6');s=await state();const before=s.balance;
      check(s.priceProducts.every(id=>id.startsWith('cig_')),'Tobacco category excludes drinks');
      await fill(await control('Price:Edit'),'2400');s=await state();
      check(s.selectedPrice===2200&&s.priceDraft==='2400','Actual typing does not commit price prematurely');
      await enter();s=await state();check(s.selectedPrice===2400&&s.balance===before,'Enter commits valid typed price without charging');
      await fill(await control('Price:Edit'),'2500');await key('Escape','Escape',27);s=await state();
      check(s.device==='Monitor'&&s.selectedPrice===2400&&!s.priceEditing,'Escape cancels price edit and keeps computer open');
      await fill(await control('Price:Edit'),'2401');await enter();s=await state();
      check(s.selectedPrice===2400&&s.priceError.length>0,'Invalid five-forint increment is rejected inline');await capture('price-invalid');
      await key('Escape','Escape',27);await press('Price:Plus');s=await state();check(s.selectedPrice===2500,'Mouse plus changes price by100');
      await send('Input.dispatchKeyEvent',{type:'keyDown',key:'Shift',code:'ShiftLeft',windowsVirtualKeyCode:16,modifiers:8});
      await press('Price:Minus');
      await send('Input.dispatchKeyEvent',{type:'keyUp',key:'Shift',code:'ShiftLeft',windowsVirtualKeyCode:16});s=await state();
      check(s.selectedPrice===2495,'Shift mouse minus changes price by5');
      await fill(await control('Price:Edit'),'2600');await press('Price:Category:1');s=await state();
      check(s.priceCategory===1&&s.priceProducts.length===3&&!s.priceProducts.includes('beer'),'Leaving price field commits and switches to soft drinks');
      await press('Price:Category:2');s=await state();check(s.priceProducts.join(',')==='kevert,beer','Alcohol category isolates the two alcoholic products');
      await press('Price:Category:3');s=await state();check(s.priceProducts.length===3&&s.priceProducts.includes('latte'),'Coffee category is enabled after actual machine purchase');
      await press('Price:Category:0');s=await state();check(s.selectedPrice===2600,'Blur-committed tobacco price survives category round trip');await capture('prices');
      for(const [app,name] of [[3,'coffee'],[4,'cameras'],[5,'games']]) {
        await press('Desktop:0');await press('OpenApp:'+app);s=await state();check(s.computerApp===({3:'Coffee',4:'Cameras',5:'Games'})[app],'Actual desktop click opens '+name);
        if(app===3){const money=s.balance;await press('Coffee:Milk');s=await state();check(s.balance===money-1500,'Coffee app orders real milk supplies');await press('Coffee:Recipe:11');check((await state()).recipe===11,'Coffee app selects physical latte recipe');}
        if(app===4){await press('Camera:1');await press('Camera:0');}
        await capture(name);
      }
      await press('Game:Memory');s=await state();check(s.computerControlIds.filter(id=>id.startsWith('Memory:')).length===16,'Actual game launcher opens sixteen memory cards');
      await press('Memory:0');await capture('memory');await press('Game:Menu');
      await press('Desktop:0');await press('OpenApp:2');await press('Arrange:0');s=await state();
      check(s.arranging,'Actual arrangement action opens unobstructed top-down editor');await capture('arrangement');await key('Escape','Escape',27);
      await probe('Prepare','visual-refresh');
      await viewport(1920,1080);
      for(let app=0;app<=6;app++) {
        if(app>0)await press('OpenApp:'+app);
        s=await state();check(s.computerControls.every(r=>r.x>=0&&r.y>=0&&r.x+r.width<=width&&r.y+r.height<=height),'All visible controls fit large viewport app'+app);
        await capture('app-'+app+'-large');
        if(app>0)await press('Desktop:0');
      }
      await probe('Action','design-exterior');await capture('exterior-large');
      await probe('Action','design-interior');await capture('interior-large');
    }
    if (selected === 'requests-batch') {
      const worldClick=async()=>{
        await send('Input.dispatchMouseEvent',{type:'mousePressed',x:width/2,y:height/2,button:'left',clickCount:1});await sleep(90);
        await send('Input.dispatchMouseEvent',{type:'mouseReleased',x:width/2,y:height/2,button:'left',clickCount:1});await sleep(300);
      };
      await viewport(1280,720);
      await probe('Prepare','requests-arrivals');await probe('Action','advance-requests-arrivals');let s=await state();
      check(s.requestsSpawned>8 && s.phase==='Open','Open shop receives visits beyond the old fixed daily limit');await capture('continuous-arrivals');
      // Test the real sign input with a clear entrance. The arrival simulation
      // can retain a departing actor directly in front of the sign while paused.
      await probe('Prepare','requests-arrivals');const visitors=(await state()).requestsSpawned;
      await probe('Action','aim-sign-stable');await key('e','KeyE',69);s=await state();
      check(s.phase==='Closing' || s.phase==='AfterHours','Actual sign interaction closes ongoing arrivals');
      await probe('Action','drain-shop');await probe('Action','advance-requests-arrivals');s=await state();
      check(s.requestsSpawned===visitors && s.phase==='AfterHours','No new arrivals after closing while existing customers leave');
      await probe('Prepare','feature-scanner');s=await state();const scans=s.scannerCount;
      await probe('Action','aim-customer-zone');await key('e','KeyE',69);s=await state();
      check(s.held==='cig_red' && s.stage==='Order' && s.customerGuides===0,'Cigarette cannot be placed directly in customer zone and no green guide appears there');await capture('customer-zone-cigarette-blocked');
      await probe('Action','aim-scanner');await worldClick();s=await state();
      check(!s.held && s.stage==='Payment' && s.scannerCount===scans+1,'Mouse puts cigarette on scanner and internal handover reaches payment');
      await viewport(1920,1080);await capture('scanner-zone-separated-large');await viewport(1280,720);
      await probe('Prepare','feature-tools');await capture('broom-corner');await key('e','KeyE',69);
      check((await state()).tool==='Mop','Actual E picks the leaning corner broom');
      await probe('Action','aim-broom-rack');s=await state();
      check(s.toolRack==='Mop','Empty broom home is a reachable return target');await capture('broom-return-target');
      await worldClick();s=await state();check(!s.tool && s.broomAtHome,'Mouse returns broom to permanent leaning home without picking it up again');
      await probe('Action','aim-bat');await key('e','KeyE',69);check((await state()).tool==='Bat','Actual E picks mounted bat');
      await probe('Action','aim-bat-rack');await key('e','KeyE',69);s=await state();
      check(!s.tool && s.batAtHome,'Actual E returns bat to its wall mount');await capture('bat-returned');
      await probe('Prepare','requests-lighting');check((await state()).fixtureLights===2,'Both physical ceiling fixtures are lit after closing');
      await probe('Action','fixture-lights-off');await capture('fixture-lights-off');
      await probe('Action','fixture-lights-on');await capture('fixture-lights-on');
      await viewport(1920,1080);await capture('fixture-lights-on-large');await viewport(1280,720);
      await probe('Prepare','requests-entrance');s=await state();const cash=s.balance;
      await send('Input.dispatchKeyEvent',{type:'keyDown',key:'w',code:'KeyW',windowsVirtualKeyCode:87});await sleep(1000);
      await send('Input.dispatchKeyEvent',{type:'keyUp',key:'w',code:'KeyW',windowsVirtualKeyCode:87});s=await state();
      check(s.position.z< -4.7 && s.phase==='AfterHours' && s.balance===cash,'Actual W walks outside with no invisible wall and no automatic day ending');await capture('outside-after-closing');
      await send('Input.dispatchKeyEvent',{type:'keyDown',key:'s',code:'KeyS',windowsVirtualKeyCode:83});await sleep(1000);
      await send('Input.dispatchKeyEvent',{type:'keyUp',key:'s',code:'KeyS',windowsVirtualKeyCode:83});s=await state();
      check(s.position.z> -4.1 && s.phase==='AfterHours','Actual S walks back through the open doorway');
      await probe('Action','aim-door');await key('e','KeyE',69);s=await state();
      check(s.phase==='Summary' && s.balance===cash-300,'Explicit E at doorway finishes the closed day exactly once');await capture('explicit-day-end');
      await probe('Prepare','requests-coffee-order');s=await state();const coffeeCash=s.balance,coffeeScans=s.scannerCount,coffeeSales=s.sales;
      for(let i=0;i<3;i++){
        await probe('Action','aim-coffee-display');await key('e','KeyE',69);s=await state();
        await clickRect(s.coffeeButtons[i]);await clickRect((await state()).coffeeButtons[3]);
        for(let wait=0;wait<40 && !(await state()).cup;wait++)await sleep(100);
        s=await state();check(!!s.cup,'Physical coffee menu brews cup '+i);
        await clickRect(s.coffeeButtons[3]);s=await state();check(!!s.held,'Physical menu gives actual cup '+i);
        await probe('Action','aim-coffee-handover-'+i);await key('e','KeyE',69);s=await state();
        check(!s.held && s.received===i+1,'Actual E serves coffee in a free customer-zone place '+i);
      }
      check(s.stage==='Payment' && s.scannerCount===coffeeScans && s.coffee===0 && s.milk===0,'Three prepared coffees reach payment without barcode scans or lost ingredients');await capture('three-coffees-served');
      await viewport(1920,1080);await capture('three-coffees-served-large');await viewport(1280,720);
      await probe('Action','focus-request-terminal');await type('2500');await enter();s=await state();
      check(s.sales===coffeeSales+1 && s.balance===coffeeCash+2500 && s.drawer===s.balance && s.coffeeMerchandise===0,'One real card approval sells all three served coffees and reconciles stock and money');
    }
    if (selected === 'new-features' || selected === 'cart-only') {
      await viewport(1280,720);
      await probe('Prepare','feature-cart'); let s=await state(); const balance=s.balance;
      check(s.clock==='08:00' && s.cartQuantities.every(q=>q===0),'Cart opens empty before the 08:00 shift');
      const fill = async (rect,value) => {
        await clickRect(rect); await key('Backspace','Backspace',8);
        await send('Input.dispatchKeyEvent',{type:'char',text:String(value),unmodifiedText:String(value)}); await sleep(300);
      };
      await fill(s.cartFields[0],2); await fill((await state()).cartFields[1],3); s=await state();
      check(s.cartQuantities[0]===2 && s.cartQuantities[1]===3 && s.balance===balance,'Actual quantity fields accept distinct typed values without purchasing');
      await click(s.cartFields[0].x+53+16,s.cartFields[0].y+15);
      check((await state()).cartQuantities[0]===3,'Mouse plus increases only the chosen product');
      await click(s.cartFields[0].x-37+16,s.cartFields[0].y+15); s=await state();
      check(s.cartQuantities[0]===2 && s.cartQuantities[1]===3,'Mouse minus decreases only the chosen product');
      await capture('cart-typed'); await clickRect(s.cartSubmit); s=await state();
      check(s.pendingCart[0]===2 && s.pendingCart[1]===3 && s.balance===balance-7800 && s.balance===s.drawer,'One mouse confirmation submits every cart row and exact total');
      await viewport(1920,1080); await capture('cart-submitted-large'); await viewport(1280,720);
      if (selected !== 'cart-only') {
      await probe('Prepare','feature-scanner'); s=await state(); const scans=s.scannerCount;
      await capture('scanner-ready'); await key('e','KeyE',69); s=await state();
      check(s.scannerCount===scans+1 && s.scannerProduct==='cig_red' && s.stage==='Payment' && !s.held,'Actual E scans one held product before transferring it to the customer');
      await viewport(1920,1080); await capture('scanner-transferred-large'); await viewport(1280,720);
      await probe('Prepare','feature-tools'); await capture('warehouse-broom'); await key('e','KeyE',69);
      check((await state()).tool==='Mop','Actual E picks up the warehouse broom');
      await probe('Action','aim-bat'); await capture('wall-bat'); await key('e','KeyE',69);
      check((await state()).tool==='Bat','Actual E picks up the mounted bat');
      await probe('Prepare','mop'); await send('Input.dispatchMouseEvent',{type:'mousePressed',x:640,y:360,button:'left',clickCount:1}); await sleep(1800);
      await send('Input.dispatchMouseEvent',{type:'mouseReleased',x:640,y:360,button:'left',clickCount:1});
      check((await state()).dirt===0,'Holding the mouse sweeps the actual stain clean'); await capture('broom-swept');
      await probe('Prepare','feature-night'); await key('e','KeyE',69); s=await state(); const nightCash=s.balance, quantity=s.offeredQuantity;
      check(s.tradeOpen && quantity>=1 && quantity<=10,'Merchant states a bounded actual shipment quantity'); await capture('night-offer');
      await click(640,450); s=await state();
      check(s.boxedQuantity===quantity && s.balance===nightCash-quantity*700,'Mouse purchases one box with exact announced quantity and cost');
      await click(640,505); await probe('Action','aim-night-box');
      await send('Input.dispatchMouseEvent',{type:'mouseMoved',x:640,y:530}); await sleep(300);
      await capture('night-box'); await key('e','KeyE',69);
      check((await state()).tool==='ContrabandBox','Actual E carries the whole shipment');
      await probe('Action','aim-private-storage'); await key('e','KeyE',69); s=await state();
      check(s.privateStock===quantity && !s.tool,'Actual E unpacks the whole shipment into the two-row shelf');
      await probe('Action','view-private-storage'); await capture('private-two-rows'); await viewport(1920,1080); await capture('private-two-rows-large'); await viewport(1280,720);
      }
    }
    if (!selected || selected === 'coffee-display' || selected === 'new-features') {
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
      check(s.recipe === 11 && s.coffeeScreenRecipe === 'Latte', 'Number key does not bypass menu recipe selection');
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

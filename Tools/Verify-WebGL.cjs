// Run: node Tools/Verify-WebGL.cjs [public URL]. Default: serve Builds/WebGL locally.
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const { spawn } = require('node:child_process');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
(async () => {
  const output = path.resolve('Builds/Verification'); fs.mkdirSync(output, { recursive: true });
  const cache = path.join(process.env.LOCALAPPDATA, 'ms-playwright');
  const chromium = fs.readdirSync(cache).find(name => /^chromium-\d+$/.test(name));
  const executable = process.env.WEBGL_BROWSER || path.join(cache, chromium, 'chrome-win64/chrome.exe');
  let server, browser, ws;
  const messages = [];
  try {
    const requestedUrl = process.argv.slice(2).find(value => !value.startsWith('--'));
    let url = requestedUrl;
    if (!url) {
      const root = path.resolve('Builds/WebGL');
      server = http.createServer((req, res) => {
        const file = path.resolve(root, '.' + new URL(req.url, 'http://localhost').pathname);
        if (!file.startsWith(root + path.sep) && file !== root) { res.writeHead(403).end(); return; }
        const resolved = file === root ? path.join(root, 'index.html') : file;
        const mime = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.png': 'image/png' };
        fs.readFile(resolved, (error, bytes) => {
          if (error) res.writeHead(404).end();
          else { res.setHeader('Content-Type', mime[path.extname(resolved)] || 'application/octet-stream'); res.end(bytes); }
        });
      });
      await new Promise(resolve => server.listen(8877, '127.0.0.1', resolve));
      url = 'http://127.0.0.1:8877/';
    }
    browser = spawn(executable, ['--headless=new', '--remote-debugging-port=9333', '--user-data-dir=' + path.join(output, 'browser-' + Date.now()), '--no-first-run', '--no-default-browser-check', '--window-size=1280,900', 'about:blank'], { windowsHide: true, stdio: 'ignore' });
    let target;
    for (let i = 0; i < 60; i++) {
      try { target = (await (await fetch('http://127.0.0.1:9333/json')).json()).find(t => t.type === 'page'); if (target) break; } catch {}
      await sleep(500);
    }
    if (!target) throw new Error('Chromium debugging endpoint did not start.');
    ws = new WebSocket(target.webSocketDebuggerUrl); await new Promise(resolve => { ws.onopen = resolve; });
    let sequence = 0; const pending = new Map();
    const send = (method, params = {}) => new Promise((resolve, reject) => { const id = ++sequence; pending.set(id, { resolve, reject }); ws.send(JSON.stringify({ id, method, params })); });
    ws.onmessage = event => {
      const m = JSON.parse(event.data);
      if (m.id) { const p = pending.get(m.id); pending.delete(m.id); m.error ? p?.reject(m.error) : p?.resolve(m.result); }
      else if (m.method === 'Runtime.consoleAPICalled') messages.push({ type: m.params.type, text: m.params.args.map(x => x.value ?? x.description).join(' ') });
      else if (m.method === 'Runtime.exceptionThrown') messages.push({ type: 'exception', detail: m.params.exceptionDetails });
      else if (m.method === 'Page.javascriptDialogOpening') { messages.push({ type: 'dialog', text: m.params.message }); send('Page.handleJavaScriptDialog', { accept: true }); }
    };
    await send('Runtime.enable'); await send('Page.enable'); await send('Page.bringToFront'); await send('Emulation.setFocusEmulationEnabled', { enabled: true }); await send('Page.navigate', { url }); await sleep(35000);
    const boundsResult = await send('Runtime.evaluate', { expression: 'JSON.stringify(document.querySelector("canvas").getBoundingClientRect().toJSON())', returnByValue: true });
    const bounds = JSON.parse(boundsResult.result.value);
    const clickAt = async (x, y) => {
      const click = { x: bounds.x + bounds.width*x, y: bounds.y + bounds.height*y, button: 'left', clickCount: 1 };
      await send('Input.dispatchMouseEvent', { ...click, type: 'mousePressed' });
      await send('Input.dispatchMouseEvent', { ...click, type: 'mouseReleased' }); await sleep(500);
    };
    const label = requestedUrl ? 'live' : 'local'; const saved = [];
    const capture = async suffix => {
      const screenshot = await send('Page.captureScreenshot', { format: 'png' });
      const file = path.join(output, label + (suffix ? '-' + suffix : '') + '.png');
      fs.writeFileSync(file, Buffer.from(screenshot.data, 'base64')); saved.push(file);
    };
    // The first welcome button is anchored above the last two buttons.
    await clickAt(.5, .72); await sleep(2500); await capture('');
    if (process.argv.includes('--tobaccos') || process.argv.includes('--features')) {
      const key = async (name, code, value) => {
        await send('Input.dispatchKeyEvent', { type: 'keyDown', key: name, code, windowsVirtualKeyCode: value });
        await sleep(100);
        await send('Input.dispatchKeyEvent', { type: 'keyUp', key: name, code, windowsVirtualKeyCode: value });
      };
      const aspect = bounds.width / bounds.height;
      const logicalHeight = Math.max(1.18, 1.95 / aspect) * 1.04;
      const logicalWidth = logicalHeight * aspect;
      const monitorPoint = (x, y) => ({ x: .5 - x / logicalWidth, y: .5 - (y + .025) / logicalHeight });
      const monitorClick = async (x, y) => { const p = monitorPoint(x, y); await clickAt(p.x, p.y); };
      await key('b', 'KeyB', 66); await sleep(1500); await capture('tobaccos-desktop');
      await monitorClick(.58, .11); await capture('tobaccos-starter');
      await monitorClick(.02, .24); await monitorClick(.58, .12); await sleep(8500); await capture('tobaccos-arrival');
      await monitorClick(.45, -.465); await monitorClick(0, .11); await capture('developments');
      if (process.argv.includes('--features')) {
        await monitorClick(.45, -.465); await monitorClick(-.58, .11); await capture('coffee');
        await monitorClick(.45, -.465); await monitorClick(.36, -.14); await sleep(1000); await capture('cameras');
        await monitorClick(.45, -.465); await monitorClick(-.36, -.14); await capture('games');
        const topLeft = monitorPoint(.87, .27); const bottomRight = monitorPoint(-.87, -.35);
        const appWidth = bottomRight.x - topLeft.x; const appHeight = bottomRight.y - topLeft.y;
        await clickAt(topLeft.x + appWidth * .25, topLeft.y + 100 / bounds.height);
        await key('ArrowUp', 'ArrowUp', 38); await sleep(600); await capture('snake');
        await clickAt(bottomRight.x - 82.5 / bounds.width, topLeft.y + 20 / bounds.height);
        await clickAt(topLeft.x + appWidth * .74, topLeft.y + 100 / bounds.height); await capture('memory');
        await clickAt(topLeft.x + appWidth * .125, topLeft.y + appHeight * .125 + 32 / bounds.height);
        await clickAt(topLeft.x + appWidth * .375, topLeft.y + appHeight * .125 + 32 / bounds.height); await capture('memory-pair');
        await key('Backspace', 'Backspace', 8); await sleep(250); await capture('keyboard-game-menu');
        await key('Backspace', 'Backspace', 8); await key('1', 'Digit1', 49); await sleep(250);
        await key('1', 'Digit1', 49); await key('Tab', 'Tab', 9); await capture('keyboard-order-focus');
        await key('Backspace', 'Backspace', 8); await key('4', 'Digit4', 52); await sleep(250); await capture('keyboard-cameras');
        await key('Backspace', 'Backspace', 8); await key('5', 'Digit5', 53); await key('Tab', 'Tab', 9);
        await key('ArrowRight', 'ArrowRight', 39); await key('Enter', 'Enter', 13); await sleep(250);
        await key('Tab', 'Tab', 9); await key('Tab', 'Tab', 9); await key(' ', 'Space', 32); await capture('keyboard-memory');
        if(process.argv.includes('--evening')){
          await key('Backspace','Backspace',8);await key('Backspace','Backspace',8);await key('6','Digit6',54);await capture('pricing');
          await key('=','Equal',187);await capture('pricing-changed');await key('-','Minus',189);
        }
      }
    }
    if (process.argv.includes('--dayend')) {
      const key = async (name, code, value) => {
        await send('Input.dispatchKeyEvent', { type: 'keyDown', key: name, code, windowsVirtualKeyCode: value }); await sleep(120);
        await send('Input.dispatchKeyEvent', { type: 'keyUp', key: name, code, windowsVirtualKeyCode: value }); await sleep(200);
      };
      await key('Escape', 'Escape', 27); await clickAt(.5, .5);
      await key('o', 'KeyO', 79); await sleep(750); await key('o', 'KeyO', 79);
      for(let i=0;i<24;i++){await key('x','KeyX',88);await sleep(500);}
      await capture('dayend-summary');
      if(process.argv.includes('--evening')){await key('s','KeyS',83);await sleep(750);await capture('after-hours');await key('o','KeyO',79);await sleep(500);await capture('evening-return');}
      await key('Enter','Enter',13);await sleep(750);await capture('next-day');
    }
    fs.writeFileSync(path.join(output, label + '-console.json'), JSON.stringify(messages, null, 2));
    const errors = messages.filter(m => ['error', 'exception', 'dialog'].includes(m.type));
    console.log(JSON.stringify({ url, screenshots: saved, errors }, null, 2));
    if (errors.length) process.exitCode = 1;
  } finally { ws?.close(); browser?.kill(); server?.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

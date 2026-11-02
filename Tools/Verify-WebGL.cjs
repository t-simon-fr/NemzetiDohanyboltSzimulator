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
    let url = process.argv[2];
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
    // First menu button enters preparation mode in the standard 960 x 600 player.
    const click = { x: 632, y: 488, button: 'left', clickCount: 1 };
    await send('Input.dispatchMouseEvent', { ...click, type: 'mousePressed' });
    await send('Input.dispatchMouseEvent', { ...click, type: 'mouseReleased' }); await sleep(3000);
    const screenshot = await send('Page.captureScreenshot', { format: 'png' });
    const label = process.argv[2] ? 'live' : 'local';
    fs.writeFileSync(path.join(output, label + '.png'), Buffer.from(screenshot.data, 'base64'));
    fs.writeFileSync(path.join(output, label + '-console.json'), JSON.stringify(messages, null, 2));
    const errors = messages.filter(m => ['error', 'exception', 'dialog'].includes(m.type));
    console.log(JSON.stringify({ url, screenshot: path.join(output, label + '.png'), errors }, null, 2));
    if (errors.length) process.exitCode = 1;
  } finally { ws?.close(); browser?.kill(); server?.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });

const http = require('http');
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..', 'WebGLBuild');
const port = Number(process.env.WEBGL_PORT || 8080);

const mimeTypes = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.wasm': 'application/wasm',
  '.data': 'application/octet-stream',
  '.json': 'application/json; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
};

function getUnityWebContentType(filePath) {
  if (filePath.endsWith('.wasm.unityweb')) {
    return 'application/wasm';
  }

  if (filePath.endsWith('.js.unityweb')) {
    return 'application/javascript; charset=utf-8';
  }

  return 'application/octet-stream';
}

function send(res, status, body, contentType = 'text/plain; charset=utf-8') {
  res.writeHead(status, { 'Content-Type': contentType });
  res.end(body);
}

const server = http.createServer((req, res) => {
  let requestPath = decodeURIComponent(req.url.split('?')[0]);
  if (requestPath === '/') {
    requestPath = '/index.html';
  }

  const filePath = path.normalize(path.join(root, requestPath));
  if (!filePath.startsWith(root)) {
    send(res, 403, 'Forbidden');
    return;
  }

  fs.readFile(filePath, (error, data) => {
    if (error) {
      send(res, 404, 'Not found');
      return;
    }

    const isUnityWeb = filePath.endsWith('.unityweb');
    const contentType = isUnityWeb
      ? getUnityWebContentType(filePath)
      : mimeTypes[path.extname(filePath)] || 'application/octet-stream';
    const headers = { 'Content-Type': contentType };

    if (isUnityWeb) {
      headers['Content-Encoding'] = 'br';
    }

    res.writeHead(200, headers);
    res.end(data);
  });
});

server.listen(port, '127.0.0.1', () => {
  console.log(`Unity WebGL server running at http://127.0.0.1:${port}/`);
  console.log(`Serving ${root}`);
});

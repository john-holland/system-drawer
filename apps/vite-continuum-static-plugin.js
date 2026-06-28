import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const MIME = {
  '.js': 'application/javascript',
  '.css': 'text/css',
  '.html': 'text/html',
  '.json': 'application/json',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
  '.woff2': 'font/woff2',
  '.map': 'application/json',
};

/** Continuum hub SPAs served by continuum_api (Flask on 5050 in production). */
const HUB_HTML = {
  '/ui': 'ui.html',
  '/lemma-library': 'lemma-library/index.html',
  '/network-definitions': 'network-definitions/index.html',
  '/city-config': 'city-config/index.html',
  '/society-dashboard': 'society-dashboard/index.html',
  '/camera-pathing': 'camera-pathing/index.html',
  '/table-read': 'table-read/index.html',
};

function resolveHubHtml(url) {
  if (HUB_HTML[url]) return HUB_HTML[url];
  for (const prefix of Object.keys(HUB_HTML)) {
    if (url.startsWith(prefix + '/')) return HUB_HTML[prefix];
  }
  return null;
}

function sendFile(res, filePath) {
  fs.readFile(filePath, (err, data) => {
    if (err) {
      res.statusCode = err.code === 'ENOENT' ? 404 : 500;
      res.end(err.code === 'ENOENT' ? 'Not found' : 'Read error');
      return;
    }
    const ext = path.extname(filePath).toLowerCase();
    res.setHeader('Content-Type', MIME[ext] || 'application/octet-stream');
    res.end(data);
  });
}

/** Serve Drawer 2 continuum_api/static during Vite dev (no Flask required for static/hub HTML). */
export function continuumStaticPlugin() {
  const staticRoot = path.resolve(
    path.dirname(fileURLToPath(import.meta.url)),
    '../Scripts/continuum_api/static',
  );

  return {
    name: 'continuum-static',
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const url = (req.url || '').split('?')[0];

        const hubRel = resolveHubHtml(url);
        if (hubRel) {
          const hubPath = path.normalize(path.join(staticRoot, hubRel));
          if (!hubPath.startsWith(staticRoot)) {
            res.statusCode = 403;
            res.end('Forbidden');
            return;
          }
          sendFile(res, hubPath);
          return;
        }

        if (!url.startsWith('/static/')) return next();

        const rel = decodeURIComponent(url.slice('/static/'.length));
        const filePath = path.normalize(path.join(staticRoot, rel));
        if (!filePath.startsWith(staticRoot)) {
          res.statusCode = 403;
          res.end('Forbidden');
          return;
        }
        sendFile(res, filePath);
      });
    },
  };
}

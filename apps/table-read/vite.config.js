import { defineConfig } from 'vite';
import { continuumStaticPlugin } from '../vite-continuum-static-plugin.js';

export default defineConfig({
  plugins: [continuumStaticPlugin()],
  server: {
    host: '127.0.0.1',
    port: 5175,
    strictPort: true,
    proxy: {
      '/api': 'http://127.0.0.1:5050',
      '/socket.io': { target: 'http://127.0.0.1:5050', ws: true },
    },
  },
  appType: 'spa',
});

import { defineConfig } from 'vite';
import { continuuuumStaticPlugin } from '../vite-continuuuum-static-plugin.js';

export default defineConfig({
  plugins: [continuuuumStaticPlugin()],
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

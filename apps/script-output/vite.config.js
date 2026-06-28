import { defineConfig } from 'vite';
import { continuumStaticPlugin } from '../vite-continuum-static-plugin.js';

export default defineConfig({
  plugins: [continuumStaticPlugin()],
  server: {
    host: '127.0.0.1',
    port: 5174,
    strictPort: true,
    proxy: {
      '/api': 'http://127.0.0.1:5050',
    },
  },
  appType: 'spa',
});

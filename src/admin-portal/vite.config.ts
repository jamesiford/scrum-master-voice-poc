import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const apiTarget = process.env['API_URL'] ?? 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  server: {
    port: Number(process.env['PORT'] ?? 5173),
    // The portal never calls the API cross-origin in dev; the proxy keeps fetch URLs relative.
    proxy: { '/api': apiTarget },
  },
});

import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    // `npm run dev` talks to the API started with `dotnet run` (http profile).
    proxy: { '/api': 'http://localhost:5198' },
  },
  build: {
    // The API serves the built app from wwwroot, so everything shares one origin.
    outDir: '../GameEventApi/GameEventApi/wwwroot',
    emptyOutDir: true,
  },
})

import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        // NOTE: Keep a local API proxy so frontend calls to "/api/*" do not hit
        // the Vite dev server (which returns 404). This also avoids browser CORS
        // preflight failures during local development by tunneling through :5173.
        target: process.env.VITE_DEV_PROXY_TARGET ?? 'https://localhost:7217',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

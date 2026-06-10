import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from 'tailwindcss'
import autoprefixer from 'autoprefixer'

// DEV: esquema puertos 4xxx — ver docs/designs/port-allocation-flit.md
// Front :4001 | Gateway (YARP) :4002 | Flit.Api interno :4003 | python-ml :4012
const GATEWAY_DEV_URL = 'http://localhost:4002'

export default defineConfig({
  plugins: [react()],
  css: {
    postcss: {
      plugins: [tailwindcss, autoprefixer],
    },
  },
  server: {
    port: 4001,
    strictPort: true,
    proxy: {
      '/api': {
        target: GATEWAY_DEV_URL,
        changeOrigin: true,
      },
      '/hubs': {
        target: GATEWAY_DEV_URL,
        changeOrigin: true,
        ws: true,
      },
      '/ml': {
        target: GATEWAY_DEV_URL,
        changeOrigin: true,
      },
    },
  },
})

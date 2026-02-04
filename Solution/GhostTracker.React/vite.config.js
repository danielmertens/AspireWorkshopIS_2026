import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [react()],
  server: {
    port: process.env.PORT || 4001,
    host: '0.0.0.0', // Allow external connections
    proxy: {
      '/bff': {
        target: process.env.services__bff__https__0 || 
                process.env.services__bff__http__0 || 
                'http://localhost:5000',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/bff/, ''),
        secure: false
      }
    }
  },
  build: {
    outDir: 'dist',
    assetsDir: 'assets',
    sourcemap: mode !== 'production'
  },
  publicDir: 'public'
}))

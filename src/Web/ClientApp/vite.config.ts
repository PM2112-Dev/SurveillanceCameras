import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const target =
  process.env['services__webapi__https__0'] ||
  process.env['services__webapi__http__0'];

const proxyOptions = target
  ? { target, secure: false, changeOrigin: true }
  : undefined;

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  css: {
    preprocessorOptions: {
      scss: {
        // Ẩn cảnh báo deprecation phát sinh từ thư viện (Pico CSS) trong node_modules.
        quietDeps: true,
        silenceDeprecations: ['if-function'],
      },
    },
  },
  server: {
    port: parseInt(process.env.PORT!),
    // Cổng bận thì báo lỗi thay vì tự nhảy sang cổng khác (đổi origin là mất token trong localStorage).
    strictPort: true,
    proxy: proxyOptions
      ? {
          '/api': proxyOptions,
          '/media': proxyOptions,
          '/openapi': proxyOptions,
          '/scalar': proxyOptions,
        }
      : undefined,
  },
  build: {
    outDir: 'build',
  },
});

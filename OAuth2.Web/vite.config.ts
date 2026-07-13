import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

const certificatePath = fileURLToPath(new URL('./.certs/localhost.pem', import.meta.url));
const certificateKeyPath = fileURLToPath(new URL('./.certs/localhost.key', import.meta.url));

export default defineConfig(({ command }) => {
  const https = command === 'serve'
    ? {
        cert: readFileSync(certificatePath),
        key: readFileSync(certificateKeyPath),
      }
    : undefined;

  return {
    plugins: [vue()],
    server: {
      host: 'localhost',
      https,
      proxy: {
        '/api': {
          target: 'https://localhost:7140',
          changeOrigin: true,
          secure: false,
        },
      },
    },
  };
});

import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import vueDevTools from 'vite-plugin-vue-devtools';

// Injected by Aspire service discovery when the AppHost references ah-webapi.
const apiTarget =
    process.env['services__ah-webapi__https__0'] ||
    process.env['services__ah-webapi__http__0'];

// https://vite.dev/config/
export default defineConfig({
    plugins: [vue(), tailwindcss(), vueDevTools()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },
    server: {
        port: Number(process.env.PORT) || 57976,
        proxy: apiTarget
            ? { '/api': { target: apiTarget, changeOrigin: true, secure: false } }
            : undefined,
    },
});

import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import vueDevTools from 'vite-plugin-vue-devtools';

// There is no dev server: Aspire runs `npm run build:watch`, which rebuilds into AH.WebApi/wwwroot,
// and AH.WebApi serves the SPA, so /api calls are same-origin with no proxy.
// https://vite.dev/config/
export default defineConfig({
    plugins: [
        // Absolute URLs (/brand/*.jpg) point at public/ and must stay plain URLs, not module imports.
        vue({ template: { transformAssetUrls: { includeAbsolute: false } } }),
        tailwindcss(),
        // Devtools only make sense in the browser; skip them under Vitest.
        ...(process.env.VITEST ? [] : [vueDevTools()]),
    ],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },
    build: {
        // AH.WebApi serves the bundle from wwwroot (UseStaticFiles + MapFallbackToFile).
        outDir: '../AH.WebApi/wwwroot',
        emptyOutDir: true,
    },
    test: {
        environment: 'jsdom',
        setupFiles: ['./vitest.setup.js'],
        include: ['src/**/*.spec.js'],
    },
});

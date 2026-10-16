# ah.webapp

AstraHive web console: Vue 3 + Vite frontend for the virtual machine orchestrator.

## Stack

Vue 3 (`<script setup>`, JavaScript), Vite, Tailwind CSS v4, Pinia, Vue Router, vue-i18n (EN/RU), shadcn-vue on reka-ui, lucide icons, IBM Plex Sans/Mono.

## Scripts

```sh
npm install
npm run dev      # standalone dev server (http://localhost:57976)
npm run build    # production build into dist/
npm run lint     # oxlint + eslint
```

Usually the app is started with the rest of the system from the repository root via `aspire run`. The AppHost registers it as `ah-webapp` and proxies `/api` to `ah-webapi`.

## Layout

```
src/
  components/
    layout/      # AppLogo, ThemeToggle, LocaleSwitcher
    ui/          # shadcn-vue primitives (button, badge, card, sheet, ...)
  i18n/          # vue-i18n setup + locales/en.json, ru.json
  layout/        # BaseLayout, Unauthorized*/Authorized* layouts, sidebar
  lib/utils.js   # cn() helper
  router/        # routes (meta.requiresAuth) + guards
  stores/        # Pinia stores
  theme/         # light/dark theme via html[data-theme]
  views/         # route-level pages
  main.css       # Tailwind entry + design tokens
```

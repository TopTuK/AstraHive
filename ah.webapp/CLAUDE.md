# Vue 3 Project -- Claude Code Instructions

## Identity

You are working on a production Vue 3 application. Follow modern Vue best
practices (2025+) using the Composition API with `<script setup>`.
Write clean, maintainable, type-safe code. Prioritize correctness, readability,
and accessibility. Do not over-engineer.

---

## Output and Efficiency

<!-- Adapted from https://github.com/drona23/claude-token-efficient -->

### Response Style

- Return code first. Explanation after, only if non-obvious.
- No sycophantic openers or closing fluff. Do not restate the question.
- No unsolicited suggestions beyond the requested scope.
- Be concise in output but thorough in reasoning.
- Use comments sparingly -- only where logic is unclear.

### Work Efficiency

- Read before writing. One focused coding pass. No write-delete-rewrite cycles.
- Do not re-read files already read unless they may have changed.
- If unsure: say so. Never guess or invent file paths.
- User instructions always override this file.

### Token Efficiency

- Targeted reads (offset/limit) over full-file reads. Use Grep/Glob for search.
- Batch independent tool calls in parallel. Prefer Edit over Write.
- Show only changed code blocks when explaining fixes.
- Omit imports the user can infer from existing patterns.
- Keep explanations to one or two sentences. Skip if code is self-explanatory.
- Budget: 50 tool calls maximum per task.

### ASCII Output

- No em dashes, smart quotes, or Unicode bullets in code output.
- Plain hyphens and straight quotes only. Copy-paste safe.

### Code Review Behavior

- State the bug. Show the fix. Stop. No compliments, no out-of-scope suggestions.

### Debugging Behavior

- Never speculate without reading the relevant code first.
- State what you found, where, and the fix. One pass. If cause is unclear: say so.

---

## Code Quality Rules

### General

- Simplest code that solves the problem. Minimal runtime cost.
- One component per `.vue` file, PascalCase naming.
- Only comment non-obvious "why" -- never "what". Remove dead code; do not comment it out.
- `const` by default. No `var`. No `enum` (use `as const` or union types).
- Prefer early returns over nested conditions.
- No abstractions for single-use operations. No speculative features.
- No error handling for scenarios that cannot happen.
- Three similar lines is better than a premature abstraction.

### Naming

- Components: PascalCase (`UserProfile.vue`)
- Composables: camelCase starting with `use` (`useAuth.js`)
- Stores: camelCase starting with `use` (`useCounterStore` in `counter.js`)
- Utilities: camelCase (`formatDate.js`)
- Types/Interfaces: PascalCase
- Constants: UPPER_SNAKE_CASE
- Booleans: prefix with `is`, `has`, `should`, `can`
- Event handlers: prefix with `handle` (`handleSubmit`)
- Emits: no prefix, past tense or verb (`submit`, `update:modelValue`)

---

## Component Rules

### Structure

- Use `<script setup">` for ALL components.
- Single responsibility. Composition via slots, not config-object props.
- Extract logic into composables when a component does too much.
- Place derived values inline with `computed()`, not in state or watchers.

### Props

- Max 5-7 props. Group related props into an object or split the component.
- `defineProps()` with interface. Defaults via `withDefaults`.
- Never spread unknown attrs onto DOM unless building a design-system primitive.

### v-model (defineModel)

- Use `defineModel` (Vue 3.4+) for two-way binding.
- Supports multiple named models via `defineModel('name', { required: true })`.

### defineOptions / defineExpose / useTemplateRef

- `defineOptions` (3.3+) sets `name` / `inheritAttrs` in `<script setup>`.
- `<script setup>` exposes nothing by default -- use `defineExpose` for parent template refs.
- Use `useTemplateRef` (3.5+) for typed template refs.

### Teleport

Use `<Teleport>` for modals, tooltips, toasts -- content that must escape parent CSS context.

### Conditional Rendering and Lists

- `v-if`/`v-else-if`/`v-else` for conditional display. `v-show` for frequent toggles.
- Never `v-if` and `v-for` on the same element. Filter with `computed` first.
- Always `:key` with a unique stable ID. Index-as-key only for static lists.
- For complex 3+ branch conditions, use a computed lookup object with `<component :is="...">`.

---

## State Management Rules

### Decision Tree

1. **Server data** -> TanStack Query (Vue). NEVER copy into `ref`/`reactive`/Pinia.
2. **Local UI state** (one component) -> `ref()` or `reactive()`
3. **Complex local state** -> `reactive()` in a composable
4. **Shared state** -> Lift to parent first. Prop drilling > 2 levels -> Pinia.
5. **App-wide complex state** -> Pinia store
6. **Persistent state** -> VueUse `useLocalStorage` or Pinia persistence plugin

### State Principles

- Keep state as local as possible. Never store derived values -- use `computed()`.
- `ref` for primitives and values you replace entirely.
- `reactive` for objects you mutate in place.
- Discriminated unions over multiple boolean flags (see TypeScript section).

### Provide/Inject Rules

- Always use `InjectionKey`. Wrap `inject` in a composable with null-check.
- Use `readonly()` when providing state consumers should not mutate.
- Scope providers narrowly.
- A `useXxx` composable should `throw` if the key is missing, so consumers fail loudly.

### Pinia Rules

- Setup store syntax (Composition API style). One store per domain.
- Do not put server data in Pinia -- use TanStack Query.

---

## Composable and Lifecycle Rules

### watch and watchEffect

- `watchEffect` for side effects that auto-track dependencies.
- `watch` for explicit tracking with old/new values.
- NEVER use `watch` to derive state (use `computed`).
- Always clean up -- `watch`/`watchEffect` return a stop function. Call it in `onUnmounted` when needed.

### Lifecycle Hooks

- `onMounted`: DOM access, third-party init, event listeners.
- `onUnmounted`: cleanup subscriptions, timers, listeners.
- Always pair setup with cleanup.

### computed

Use for ALL derived values -- it is automatically memoized. Vue does NOT need `useMemo` / `useCallback`.

### Composables

- Extract when: same logic in 2+ components, component too complex, needs independent testing.
- Single responsibility. Descriptive names (`useUserPermissions`, not `useData`).
- Use `toValue()` + `MaybeRefOrGetter` for flexible parameters.
- For 3+ data-fetching composables with same shape, extract a factory.

---

## Data Fetching Rules

- Always TanStack Query (Vue) or VueUse `useFetch`. Never raw `onMounted` + `ref`.
- Sensible defaults: `staleTime: 5min`, `retry: false` for 404/403.
- Typed composable wrappers for every API endpoint.
- Use `enabled: computed(() => !!id.value)` for conditional fetching.
- Invalidate queries in mutation `onSuccess` via `queryClient.invalidateQueries`.
- Handle loading, error, and empty states in every fetching component.

---

## Error Handling Rules

- Build and use `ErrorBoundary.vue` using `onErrorCaptured`. Place at app / feature / list-item level.
- Set `app.config.errorHandler` in `main.js` for global logging (Sentry, etc.).
- `try/catch` in event handlers and async functions.
- Custom `ApiError` class with `status`, `endpoint`, `message`, optional `fieldErrors`.
- Map 422 validation errors to per-field form errors; show everything else as a user-friendly toast.
- Never expose raw errors or stack traces to end users.

---

## Performance Rules

### Mandatory

- Route-level code splitting via dynamic imports (`() => import('./pages/Foo.vue')`) in Vue Router.
- Stable `:key` on all `v-for` (see Lists section).
- Cleanup all subscriptions, timers, listeners in `onUnmounted`.

### Apply When Relevant

- `KeepAlive` for caching route or tab views. `v-once` for never-changing content.
- `v-memo` for large list re-renders.
- `shallowRef`/`shallowReactive` for large objects without deep reactivity needs.
- Debounce rapid inputs (200-500ms) with `useDebounceFn` from VueUse.

### Do NOT

- Prematurely optimize. Correct first, then fast.
- Vue's reactivity is automatic -- no manual memoization like `React.memo`. Just define the function.

---

## Styling Rules

### If Using Tailwind CSS

- Use `clsx` + `tailwind-merge` via a `cn()` utility in `utils/cn.js`.
- Sort classes with `prettier-plugin-tailwindcss`.
- Compose conditional classes with `cn(...)`, not inline ternary arrays in templates.

### If Using Scoped Styles (Vue Built-in)

- `<style scoped>` for component-scoped CSS. `<style module>` for CSS Modules.
- `v-bind()` in CSS for dynamic values from script.
- Use `:deep(...)` to target child-component classes from a parent.

### General

- No runtime CSS-in-JS in new projects. No inline styles except for dynamic values.
- Use CSS custom properties (`var(--spacing-md)`, etc.) for theme values.

---

## Accessibility Rules (Non-Negotiable)

- Semantic HTML: `button` for actions, `a` for navigation, `nav`/`main`/`header`/`footer`.
- NEVER `div @click` / `span @click` for interactive elements.
- Every `img` has `alt` (use `alt=""` for decorative).
- Every interactive element is keyboard accessible.
- Every icon-only button has `aria-label`. Decorative icons get `aria-hidden="true"`.
- Every form input has `label` (via `for`) or `aria-label`.
- Use `for` in Vue, NOT React's `htmlFor`.

### SSR-Safe IDs (useId)

Use `useId()` (Vue 3.5+) in reusable components for label/input association. Never `Math.random()` (breaks SSR hydration).

### Recommended Component Libraries

| Library | Style | Best For |
|---|---|---|
| Headless UI (Vue) | Unstyled | Tailwind projects |
| Radix Vue | Unstyled | Maximum flexibility |
| PrimeVue | Styled/Themeable | Full-featured |
| Naive UI | Styled | TypeScript-first |
| shadcn-vue | Pre-styled (Tailwind) | Rapid development |

Install and enforce `eslint-plugin-vuejs-accessibility`.

---

## Security Rules (Non-Negotiable)

- NEVER `v-html` with user content. Sanitize with DOMPurify first.
- Enforce `vue/no-v-html: warn` in ESLint.
- Validate and constrain all user inputs.
- Never store auth tokens in localStorage for public apps -- use HttpOnly cookies.
- Never put secrets in frontend code. Never interpolate user input into `href` (validate URL protocol to prevent `javascript:` attacks).
- Run `npm audit` regularly. Use CSP headers in production.

---

## Project Structure Rules

```
src/
  api/                # API client, error classes
  assets/             # Static assets (images, fonts)
  components/         # Shared reusable UI components
  composables/
    data/             # Data-fetching composables (Query wrappers)
  content/            # i18n strings, translations
  layouts/            # Layout components
  pages/              # Route-level page components
    FeatureName/
      components/     # Feature-specific components
      composables/    # Feature-specific composables
  plugins/            # Vue plugins
  router/             # Route definitions, guards
  stores/             # Pinia stores
  types/              # Shared TypeScript type definitions
  utils/              # Pure utility functions
```

- Colocate feature-specific code. Promote to top-level only when shared by 2+ features.
- Enforce `import/no-cycle: error`. Use `simple-import-sort`.
- Use path aliases (`@/components`, `@/composables`) -- never `../../../` spaghetti.

---

## Testing Rules

- Vitest as runner. `@testing-library/vue` for components. Playwright for E2E.
- Test behavior, not implementation. Query by role > label > text > testId.
- Use `userEvent`, not `fireEvent`. Don't test CSS classes or internal state.
- Priority: integration tests for critical flows > unit tests for composables/utils > E2E for critical path.

---

## Tooling Rules

- TypeScript strict mode. Prettier. Husky + lint-staged.
- ESLint with: `eslint-plugin-vue` (flat/recommended), `@vue/eslint-config-typescript`, `eslint-plugin-vuejs-accessibility`, `eslint-plugin-simple-import-sort`, `eslint-plugin-import` (no-cycle).
- Required rules: `vue/no-v-html: warn`, `vue/component-name-in-template-casing: [error, PascalCase]`, `vue/define-macros-order: error`, `import/no-cycle: error`, `simple-import-sort/imports: error`, `no-console: [warn, { allow: [warn, error] }]`.

---

## Build and Deployment Rules

- Never hardcode API URLs. Use `import.meta.env.VITE_API_URL`.
- Prefix client vars with `VITE_` (Vite) or `NUXT_PUBLIC_` (Nuxt).
- Commit `.env.example`, never `.env`.
- No source maps in production unless behind auth.
- Set `chunkSizeWarningLimit` and `manualChunks` for vendor splitting in `vite.config.js`.
- CI must run: `tsc --noEmit` (or `vue-tsc --noEmit`), `eslint`, `test`, `build`.
- Do not modify CI/CD files without explicit approval.

---

## Large Project Maintenance

- `npm audit` monthly. Fix critical/high immediately. Update incrementally (one major per PR).
- Remove unused deps (`npx depcheck`). Delete dead code -- git has history.
- Refactor only when asked or required for task safety. Never mix refactor with features.
- Incremental migration: one module at a time. Document canonical pattern here.
- Set bundle size limits in CI. Check bundlephobia before installing. Prefer smaller alternatives:

| Instead of | Consider | Saving |
|---|---|---|
| moment (300KB) | date-fns or dayjs (2KB) | ~95% |
| lodash (70KB) | Native JS or lodash-es | ~90% |
| axios (13KB) | Native fetch + wrapper | ~100% |
| uuid (3KB) | crypto.randomUUID() | Zero dep |
| Vuex (~10KB) | Pinia (~1.5KB) | ~85% |

---

## When Creating a New Vue Project

1. Scaffold with `npm create vue@latest` (TypeScript, Router, Pinia, ESLint, Prettier).
2. Enable `strict: true` in tsconfig.
3. Configure ESLint with vue, vuejs-accessibility, simple-import-sort plugins.
4. Install Prettier + `prettier-plugin-tailwindcss`. Set up Husky + lint-staged.
5. Install TanStack Query (Vue) or configure VueUse `useFetch`.
6. Create `ErrorBoundary.vue` using `onErrorCaptured`.
7. Install a headless UI library (Radix Vue, PrimeVue, or shadcn-vue).
8. Create the directory structure above and a `cn()` utility (if using Tailwind).
9. Create typed provide/inject composable pattern.
10. Set up route-level code splitting with dynamic imports.
11. Add at least one integration test for the critical path.
12. Add error boundaries at app and feature levels.
13. Create `.env.example` with placeholder values.
14. Set up CI pipeline: type check, lint, test, build.

---

## When Working on an Existing Vue Project

1. Read the relevant files first. Understand existing patterns.
2. Follow existing conventions, even if they differ from these rules.
3. Do not refactor unrelated code. Do not add types/comments to unchanged code.
4. Do not introduce new patterns or libraries without approval.
5. Match existing naming, organization, and style. Test changes.

---

## Checklist Before Completing Any Task

- [ ] TypeScript compiles with zero errors (`vue-tsc --noEmit`)
- [ ] ESLint passes (`npx eslint .`)
- [ ] All existing tests pass (`npm test`)
- [ ] New code has error handling (`try/catch` for async, error boundaries for render)
- [ ] New interactive elements are keyboard accessible
- [ ] New images have alt text
- [ ] No `any` types added without justification
- [ ] No commented-out code, no `console.log` left in production code
- [ ] `v-for` items use stable, unique `:key`
- [ ] No hardcoded API URLs or secrets -- environment variables used
- [ ] No CI/CD pipeline files modified without explicit approval
- [ ] New dependencies justified (checked size, no smaller alternative)

---

## Project-Specific Overrides

### Rich text & auth (security)

- **Never use `v-html`.** Instructor-authored course/lesson HTML is rendered with the global
  `v-safe-html` directive (`src/directives/vSafeHtml.js` → DOMPurify via `src/lib/sanitizeHtml.js`).
  The server also sanitises on write (`LF.Application` `GanssHtmlSanitizer`); keep the two
  allow-lists in sync. `eslint`'s `vue/no-v-html` stays enabled.
- The session token is in an **HttpOnly cookie** — there is no `js-cookie` dependency. `api.js`
  relies on `withCredentials`; auth state comes from `authStore.ensureInitialized()` (a `/Profile`
  probe). A 401 response redirects to `/login` unless the caller passes `skipAuthRedirect`.
- `index.html` must stay free of inline `<script>`/`<style>` — the prod CSP is `script-src 'self'`
  plus `https://www.googletagmanager.com` (the theme bootstrap lives in `public/theme-init.js`).

### Analytics (Google Analytics 4)

- `vue-gtag` is imported **only** by `src/lib/analytics.js`; everything else calls `track(name, params)`.
  It sends nothing unless it's a production build (`import.meta.env.PROD`) **and** the user accepted
  `AnalyticsConsentBanner` (choice in localStorage `leanforge-analytics-consent`, resettable on `/cookies`).
- Page views strip the query string except `utm_*`/`gclid` (Robokassa's signed return params must never
  reach Google). Never send user id, email or name — only the `role` user property.
- New GA hosts need matching `script-src`/`connect-src` entries in the prod CSP in `LF.WebApi/Program.cs`.
  Any new cookie/tracking use must also be reflected in the `cookies.*` policy text (en + ru).

### Testing (actual setup)

- Runner: **Vitest** (`vite.config.js` `test` block, `jsdom`, `globals: true`, `vitest.setup.js`).
- Component tests use **`@testing-library/vue`** + `@testing-library/user-event`; `@testing-library/jest-dom` matchers.
- Specs are **co-located** as `*.spec.js` next to the file under test. `src/test/renderComponent.js` wraps `render` with the real `@/i18n` instance (locale forced to `en`) and an optional `@pinia/testing` Pinia.
- Pinia stores: `setActivePinia(createPinia())` + `vi.mock` the service modules. `authStore` calls `useRouter()` at setup — `vi.mock('vue-router')` when testing it directly.
- `vitest.setup.js` polyfills `URL.createObjectURL`/`revokeObjectURL` and `matchMedia` (jsdom lacks them; `theme/index.js` and several stores/services use them at import/runtime).
- Scripts: `npm test` (run once), `npm run test:watch`, `npm run test:coverage`, `npm run lint:ci` (eslint, no `--fix`).
- CI: `.github/workflows/webapp-tests.yml` runs lint + test + build on PRs to `main` touching `lf.webapp/**`.

<!--
Customize this section for your specific project. Examples:

### State Management
- This project uses Pinia for global state
- Server state uses TanStack Query (Vue) with 10-minute staleTime

### Styling
- This project uses CSS Modules via <style module>, not Tailwind
- Class names follow BEM convention

### API
- All API calls go through src/api/client.js
- Use the createDataComposable factory for new endpoints

### Testing
- Minimum 80% coverage on new code
- E2E tests required for all user-facing flows

### Deployment
- Do not modify CI/CD pipeline files without approval
- All PRs require passing checks before merge
-->
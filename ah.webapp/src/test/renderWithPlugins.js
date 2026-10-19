import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import { render } from '@testing-library/vue';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory, createRouter } from 'vue-router';
import { i18n } from '@/i18n';

const Stub = { template: '<div />' };

// Minimal route table: views under test only need the named routes they link to.
const TEST_ROUTES = [
    { path: '/', name: 'Home', component: Stub },
    { path: '/login', name: 'Login', component: Stub },
    { path: '/welcome', name: 'Welcome', component: Stub },
    { path: '/profile', name: 'Profile', component: Stub },
];

/**
 * Renders a component with the real i18n (English), a fresh Pinia, a fresh TanStack Query client
 * (no retries) and a memory router.
 * Returns Testing Library's result plus `pinia` and `router` for assertions.
 */
export async function renderWithPlugins(component, { route = '/', pinia = createPinia(), ...options } = {}) {
    setActivePinia(pinia);
    i18n.global.locale.value = 'en';

    const router = createRouter({ history: createMemoryHistory(), routes: TEST_ROUTES });
    await router.push(route);
    await router.isReady();

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

    const result = render(component, {
        ...options,
        global: { plugins: [pinia, i18n, router, [VueQueryPlugin, { queryClient }]], ...options.global },
    });
    return { ...result, pinia, router, queryClient };
}

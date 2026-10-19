import { createRouter, createWebHistory } from 'vue-router';
import { i18n } from '@/i18n';
import { useAuthStore } from '@/stores/authStore';

const PlaceholderView = () => import('@/views/PlaceholderView.vue');

const routes = [
    {
        path: '/',
        name: 'Home',
        component: () => import('@/views/HomeView.vue'),
        meta: { title: 'nav.home', requiresAuth: false },
    },
    {
        path: '/login',
        name: 'Login',
        component: () => import('@/views/LoginView.vue'),
        meta: { title: 'nav.signIn', requiresAuth: false },
    },
    {
        path: '/welcome',
        name: 'Welcome',
        component: () => import('@/views/WelcomeView.vue'),
        meta: { title: 'nav.welcome', requiresAuth: true },
    },
    {
        path: '/dashboard',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/DashboardView.vue'),
        meta: { title: 'nav.dashboard', requiresAuth: true },
    },
    {
        path: '/clusters',
        name: 'Clusters',
        component: PlaceholderView,
        meta: { title: 'nav.clusters', requiresAuth: true, image: 'clusters' },
    },
    {
        path: '/machines',
        name: 'Machines',
        component: PlaceholderView,
        meta: { title: 'nav.machines', requiresAuth: true, image: 'machines' },
    },
    {
        path: '/sessions',
        name: 'Sessions',
        component: PlaceholderView,
        meta: { title: 'nav.sessions', requiresAuth: true, image: 'sessions' },
    },
    {
        path: '/tasks',
        name: 'Tasks',
        component: PlaceholderView,
        meta: { title: 'nav.tasks', requiresAuth: true, image: 'tasks' },
    },
    {
        path: '/settings',
        name: 'Settings',
        component: PlaceholderView,
        meta: { title: 'nav.settings', requiresAuth: true, image: 'settings' },
    },
    {
        path: '/profile',
        name: 'Profile',
        component: () => import('@/views/profile/ProfileView.vue'),
        meta: { title: 'nav.profile', requiresAuth: true },
    },
    {
        path: '/:pathMatch(.*)*',
        name: 'NotFound',
        component: () => import('@/views/NotFoundView.vue'),
        meta: { title: 'notFound.title', requiresAuth: false },
    },
];

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
    scrollBehavior(to, _from, savedPosition) {
        if (savedPosition) return savedPosition;
        if (to.hash) return { el: to.hash };
        return { top: 0 };
    },
});

router.beforeEach(async (to) => {
    const authStore = useAuthStore();
    await authStore.ensureInitialized();

    if (to.meta.requiresAuth && !authStore.isAuthenticated) {
        return { name: 'Login' };
    }
    if (authStore.isAuthenticated && ['Home', 'Login'].includes(to.name)) {
        return { name: 'Welcome' };
    }
});

router.afterEach((to) => {
    const title = to.meta.title ? i18n.global.t(to.meta.title) : null;
    const appName = i18n.global.t('app.name');
    document.title = title ? `${title} - ${appName}` : appName;
});

export default router;

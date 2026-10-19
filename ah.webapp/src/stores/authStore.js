import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import * as authService from '@/services/authService';

// Session lives in an HttpOnly cookie issued by ah-webapi after Google/Yandex sign-in;
// the store only mirrors who the cookie belongs to.
export const useAuthStore = defineStore('auth', () => {
    const user = ref(null);
    const initialized = ref(false);
    let initPromise = null;

    const isAuthenticated = computed(() => user.value !== null);

    const ensureInitialized = () => {
        if (initialized.value) return Promise.resolve();
        if (initPromise) return initPromise;

        initPromise = (async () => {
            try {
                user.value = (await authService.fetchMe()) ?? null;
            } catch {
                user.value = null;
            } finally {
                initialized.value = true;
                initPromise = null;
            }
        })();
        return initPromise;
    };

    const loginWith = (provider, returnUrl = '/welcome') => {
        window.location.assign(authService.loginUrl(provider, returnUrl));
    };

    const logout = async () => {
        try {
            await authService.logout();
        } finally {
            user.value = null;
        }
    };

    // Mirrors a saved profile edit; the backend reissues the session cookie with the same values.
    const applyProfile = ({ firstName, lastName }) => {
        if (!user.value) return;
        const name = `${firstName} ${lastName}`.trim() || user.value.email;
        user.value = { ...user.value, firstName, lastName, name };
    };

    return { user, initialized, isAuthenticated, ensureInitialized, loginWith, logout, applyProfile };
});

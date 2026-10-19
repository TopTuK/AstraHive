import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import * as authService from '@/services/authService';
import { useAuthStore } from './authStore';

vi.mock('@/services/authService', () => ({
    fetchMe: vi.fn(),
    logout: vi.fn(),
    loginUrl: vi.fn((provider, returnUrl) => `/api/auth/login/${provider}?returnUrl=${returnUrl}`),
}));

const USER = { id: 1, email: 'john@example.com', firstName: 'John', lastName: 'Doe', name: 'John Doe', avatarUrl: null };

describe('authStore', () => {
    beforeEach(() => {
        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('starts unauthenticated and uninitialized', () => {
        const store = useAuthStore();

        expect(store.user).toBeNull();
        expect(store.isAuthenticated).toBe(false);
        expect(store.initialized).toBe(false);
    });

    it('ensureInitialized loads the current user', async () => {
        authService.fetchMe.mockResolvedValue(USER);
        const store = useAuthStore();

        await store.ensureInitialized();

        expect(store.user).toEqual(USER);
        expect(store.isAuthenticated).toBe(true);
        expect(store.initialized).toBe(true);
    });

    it('ensureInitialized treats a failed probe as signed out', async () => {
        authService.fetchMe.mockRejectedValue({ response: { status: 401 } });
        const store = useAuthStore();

        await store.ensureInitialized();

        expect(store.user).toBeNull();
        expect(store.initialized).toBe(true);
    });

    it('ensureInitialized shares one request between concurrent callers', async () => {
        authService.fetchMe.mockResolvedValue(USER);
        const store = useAuthStore();

        await Promise.all([store.ensureInitialized(), store.ensureInitialized(), store.ensureInitialized()]);

        expect(authService.fetchMe).toHaveBeenCalledTimes(1);
    });

    it('ensureInitialized does not probe again once initialized', async () => {
        authService.fetchMe.mockResolvedValue(USER);
        const store = useAuthStore();
        await store.ensureInitialized();

        await store.ensureInitialized();

        expect(authService.fetchMe).toHaveBeenCalledTimes(1);
    });

    it('logout clears the user', async () => {
        authService.logout.mockResolvedValue({});
        const store = useAuthStore();
        store.user = USER;

        await store.logout();

        expect(authService.logout).toHaveBeenCalledOnce();
        expect(store.isAuthenticated).toBe(false);
    });

    it('logout clears the user even when the request fails', async () => {
        authService.logout.mockRejectedValue(new Error('Network Error'));
        const store = useAuthStore();
        store.user = USER;

        await expect(store.logout()).rejects.toThrow('Network Error');

        expect(store.user).toBeNull();
    });

    it('loginWith navigates the browser to the provider challenge', () => {
        const assign = vi.fn();
        vi.stubGlobal('location', { assign });
        const store = useAuthStore();

        store.loginWith('google');

        expect(authService.loginUrl).toHaveBeenCalledWith('google', '/welcome');
        expect(assign).toHaveBeenCalledWith('/api/auth/login/google?returnUrl=/welcome');
    });

    it('applyProfile updates the names of the signed-in user', () => {
        const store = useAuthStore();
        store.user = { ...USER };

        store.applyProfile({ firstName: 'Jane', lastName: '' });

        expect(store.user).toMatchObject({ firstName: 'Jane', lastName: '', name: 'Jane', email: USER.email });
    });

    it('applyProfile does nothing when signed out', () => {
        const store = useAuthStore();

        store.applyProfile({ firstName: 'Jane', lastName: 'Smith' });

        expect(store.user).toBeNull();
    });
});

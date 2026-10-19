import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as authService from '@/services/authService';
import router from './index';

vi.mock('@/services/authService', () => ({
    fetchMe: vi.fn(),
    logout: vi.fn(),
    loginUrl: vi.fn(),
}));

const USER = { id: 1, email: 'john@example.com', firstName: 'John', lastName: 'Doe', name: 'John Doe', avatarUrl: null };

// The guard reads the store from the active Pinia, so each test gets a fresh,
// uninitialized store and the guard performs its own session probe.
async function navigate(path) {
    await router.push(path);
    return router.currentRoute.value;
}

describe('router auth guard', () => {
    beforeEach(async () => {
        // Leave the previous test's route on a throwaway signed-out store,
        // then hand the guard a fresh store for the test itself.
        setActivePinia(createPinia());
        authService.fetchMe.mockRejectedValue({ response: { status: 401 } });
        await router.replace('/__reset__');

        setActivePinia(createPinia());
        vi.clearAllMocks();
    });

    describe('signed out', () => {
        beforeEach(() => {
            authService.fetchMe.mockRejectedValue({ response: { status: 401 } });
        });

        it('redirects protected routes to Login', async () => {
            expect((await navigate('/welcome')).name).toBe('Login');
        });

        it.each(['/', '/login'])('allows public route %s', async (path) => {
            expect((await navigate(path)).path).toBe(path);
        });
    });

    describe('signed in', () => {
        beforeEach(() => {
            authService.fetchMe.mockResolvedValue(USER);
        });

        it.each(['/', '/login'])('sends %s to Welcome', async (path) => {
            expect((await navigate(path)).name).toBe('Welcome');
        });

        it('allows protected routes', async () => {
            expect((await navigate('/dashboard')).name).toBe('Dashboard');
        });

        it('probes the session only once across navigations', async () => {
            await navigate('/welcome');
            await navigate('/dashboard');

            expect(authService.fetchMe).toHaveBeenCalledTimes(1);
        });
    });

    it('sets the document title from the route meta', async () => {
        authService.fetchMe.mockRejectedValue({ response: { status: 401 } });

        await navigate('/login');

        expect(document.title).toBe('Sign in - Astra Hive');
    });
});

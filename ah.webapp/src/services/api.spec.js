import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from './api';

// Exercise the registered 401 interceptor directly instead of going over the network.
const onRejected = api.interceptors.response.handlers[0].rejected;

const httpError = (status, config = {}) => ({ response: { status }, config });

describe('api 401 interceptor', () => {
    let assign;

    beforeEach(() => {
        assign = vi.fn();
        vi.stubGlobal('location', { pathname: '/welcome', assign });
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('is configured for same-origin cookie requests', () => {
        expect(api.defaults.baseURL).toBe('/api');
        expect(api.defaults.withCredentials).toBe(true);
    });

    it('redirects to /login on 401 and still rejects', async () => {
        const error = httpError(401);

        await expect(onRejected(error)).rejects.toBe(error);
        expect(assign).toHaveBeenCalledWith('/login');
    });

    it('does not redirect when the request opts out with skipAuthRedirect', async () => {
        await expect(onRejected(httpError(401, { skipAuthRedirect: true }))).rejects.toBeDefined();
        expect(assign).not.toHaveBeenCalled();
    });

    it('does not redirect when already on the login page', async () => {
        window.location.pathname = '/login';

        await expect(onRejected(httpError(401))).rejects.toBeDefined();
        expect(assign).not.toHaveBeenCalled();
    });

    it.each([400, 403, 500])('does not redirect on %i', async (status) => {
        await expect(onRejected(httpError(status))).rejects.toBeDefined();
        expect(assign).not.toHaveBeenCalled();
    });

    it('does not redirect on network errors without a response', async () => {
        await expect(onRejected(new Error('Network Error'))).rejects.toBeDefined();
        expect(assign).not.toHaveBeenCalled();
    });
});

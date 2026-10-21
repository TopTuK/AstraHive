import { api } from './api';

export const fetchMe = () =>
    api.get('/auth/me', { skipAuthRedirect: true }).then((response) => response.data);

export const logout = () => api.post('/auth/logout');

// The OAuth flow is a full-page navigation, not an XHR: the backend redirects to the provider.
export const loginUrl = (provider, returnUrl) => {
    const query = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : '';
    return `/api/auth/login/${encodeURIComponent(provider)}${query}`;
};

// What the login page offers (external providers allowed by the ah.external_login flag, certificate
// sign-in, registration). Anonymous, so it never triggers the sign-in redirect.
export const fetchSignInOptions = () =>
    api.get('/auth/options', { skipAuthRedirect: true }).then((response) => response.data);

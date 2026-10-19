import { api } from './api';

export const fetchMe = () =>
    api.get('/auth/me', { skipAuthRedirect: true }).then((response) => response.data);

export const logout = () => api.post('/auth/logout');

// The OAuth flow is a full-page navigation, not an XHR: the backend redirects to the provider.
export const loginUrl = (provider, returnUrl) => {
    const query = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : '';
    return `/api/auth/login/${encodeURIComponent(provider)}${query}`;
};

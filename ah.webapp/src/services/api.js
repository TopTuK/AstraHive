import axios from 'axios';

// Same-origin requests: AH.WebApi serves both the SPA and /api,
// so the HttpOnly session cookie travels with every call.
export const api = axios.create({
    baseURL: '/api',
    withCredentials: true,
});

// An expired session on a protected call sends the user back to sign in.
// Probes such as the initial "who am I" request opt out with `skipAuthRedirect`.
api.interceptors.response.use(
    (response) => response,
    (error) => {
        const status = error?.response?.status;
        const skip = error?.config?.skipAuthRedirect;
        if (status === 401 && !skip && !window.location.pathname.startsWith('/login')) {
            window.location.assign('/login');
        }
        return Promise.reject(error);
    },
);

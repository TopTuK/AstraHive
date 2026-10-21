import { screen } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as authService from '@/services/authService';
import { useAuthStore } from '@/stores/authStore';
import { renderWithPlugins } from '@/test/renderWithPlugins';
import LoginView from './LoginView.vue';

vi.mock('@/services/authService', async (importOriginal) => ({
    ...(await importOriginal()),
    fetchSignInOptions: vi.fn(),
}));

const OPEN = { externalProviders: ['google', 'yandex'], certificate: true, registration: true };

describe('LoginView', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        authService.fetchSignInOptions.mockResolvedValue(OPEN);
    });

    it('renders a sign-in button per allowed provider', async () => {
        await renderWithPlugins(LoginView, { route: '/login' });

        expect(await screen.findByRole('button', { name: 'Continue with Google' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Continue with Yandex' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Sign in with a certificate' })).toBeInTheDocument();
        expect(screen.getByText(/created on your first sign-in/)).toBeInTheDocument();
    });

    it('offers only the certificate while external sign-in is off', async () => {
        authService.fetchSignInOptions.mockResolvedValue({ externalProviders: [], certificate: true, registration: true });
        await renderWithPlugins(LoginView, { route: '/login' });

        expect(await screen.findByText('Sign in with your Astra Hive client certificate.')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Sign in with a certificate' })).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Continue with Google' })).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Continue with Yandex' })).not.toBeInTheDocument();
    });

    it('still offers the certificate when the options cannot be loaded', async () => {
        authService.fetchSignInOptions.mockRejectedValue(new Error('offline'));
        await renderWithPlugins(LoginView, { route: '/login' });

        expect(screen.getByRole('button', { name: 'Sign in with a certificate' })).toBeInTheDocument();
        expect(screen.queryByRole('button', { name: 'Continue with Google' })).not.toBeInTheDocument();
    });

    it.each([
        ['Continue with Google', 'google'],
        ['Continue with Yandex', 'yandex'],
        ['Sign in with a certificate', 'certificate'],
    ])('clicking "%s" starts the %s sign-in', async (label, provider) => {
        const { pinia } = await renderWithPlugins(LoginView, { route: '/login' });
        const store = useAuthStore(pinia);
        const loginWith = vi.spyOn(store, 'loginWith').mockImplementation(() => {});

        await userEvent.click(await screen.findByRole('button', { name: label }));

        expect(loginWith).toHaveBeenCalledWith(provider);
    });

    it('shows no error without an error query', async () => {
        await renderWithPlugins(LoginView, { route: '/login' });

        expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });

    it('explains a missing email', async () => {
        await renderWithPlugins(LoginView, { route: '/login?error=email_required' });

        expect(screen.getByRole('alert')).toHaveTextContent('did not share your email address');
    });

    it.each([
        ['email_invalid', 'not valid'],
        ['email_unverified', 'not verified with the provider'],
    ])('explains the %s email error', async (error, message) => {
        await renderWithPlugins(LoginView, { route: `/login?error=${error}` });

        expect(screen.getByRole('alert')).toHaveTextContent(message);
    });

    it.each([
        ['certificate_required', 'No certificate was selected'],
        ['certificate_invalid', 'not issued by a trusted Astra Hive authority'],
        ['certificate_unavailable', 'Certificate sign-in is not configured'],
        ['certificate_unknown', 'not registered, has been revoked'],
    ])('explains the %s certificate error', async (error, message) => {
        await renderWithPlugins(LoginView, { route: `/login?error=${error}` });

        expect(screen.getByRole('alert')).toHaveTextContent(message);
    });

    it.each([
        ['external_login_disabled', 'Google or Yandex is turned off'],
        ['registration_disabled', 'registration is closed'],
    ])('explains the %s error', async (error, message) => {
        await renderWithPlugins(LoginView, { route: `/login?error=${error}` });

        expect(screen.getByRole('alert')).toHaveTextContent(message);
    });

    it('shows the generic message for an unknown error code', async () => {
        await renderWithPlugins(LoginView, { route: '/login?error=<script>' });

        expect(screen.getByRole('alert')).toHaveTextContent('Sign-in failed. Please try again.');
    });
});

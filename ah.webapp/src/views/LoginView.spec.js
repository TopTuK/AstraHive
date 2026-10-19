import { screen } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { useAuthStore } from '@/stores/authStore';
import { renderWithPlugins } from '@/test/renderWithPlugins';
import LoginView from './LoginView.vue';

describe('LoginView', () => {
    it('renders a sign-in button per provider', async () => {
        await renderWithPlugins(LoginView, { route: '/login' });

        expect(screen.getByRole('button', { name: 'Continue with Google' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Continue with Yandex' })).toBeInTheDocument();
    });

    it.each([
        ['Continue with Google', 'google'],
        ['Continue with Yandex', 'yandex'],
    ])('clicking "%s" starts the %s sign-in', async (label, provider) => {
        const { pinia } = await renderWithPlugins(LoginView, { route: '/login' });
        const store = useAuthStore(pinia);
        const loginWith = vi.spyOn(store, 'loginWith').mockImplementation(() => {});

        await userEvent.click(screen.getByRole('button', { name: label }));

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

    it('shows the generic message for an unknown error code', async () => {
        await renderWithPlugins(LoginView, { route: '/login?error=<script>' });

        expect(screen.getByRole('alert')).toHaveTextContent('Sign-in failed. Please try again.');
    });
});

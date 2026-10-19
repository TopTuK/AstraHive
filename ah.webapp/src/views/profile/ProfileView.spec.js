import { screen, waitFor } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as profileService from '@/services/profileService';
import { useAuthStore } from '@/stores/authStore';
import { renderWithPlugins } from '@/test/renderWithPlugins';
import ProfileView from './ProfileView.vue';

vi.mock('@/services/profileService', () => ({
    fetchProfile: vi.fn(),
    updateProfile: vi.fn(),
}));

const PROFILE = {
    id: 1,
    email: 'john@example.com',
    firstName: 'John',
    lastName: 'Doe',
    avatarUrl: null,
    createdAt: '2026-01-15T10:00:00Z',
};

async function renderProfile() {
    const { pinia } = await renderWithPlugins(ProfileView, { route: '/profile' });
    const authStore = useAuthStore(pinia);
    authStore.user = { id: 1, email: PROFILE.email, firstName: 'John', lastName: 'Doe', name: 'John Doe', avatarUrl: null };
    await screen.findByLabelText('First name');
    return authStore;
}

describe('ProfileView', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        profileService.fetchProfile.mockResolvedValue(PROFILE);
    });

    it('prefills the form from the profile', async () => {
        await renderProfile();

        expect(screen.getByLabelText('First name')).toHaveValue('John');
        expect(screen.getByLabelText('Last name')).toHaveValue('Doe');
        expect(screen.getByLabelText('Email')).toHaveValue('john@example.com');
        expect(screen.getByRole('heading', { name: 'John Doe' })).toBeInTheDocument();
    });

    it('disables saving until a name changes', async () => {
        await renderProfile();

        expect(screen.getByRole('button', { name: 'Save changes' })).toBeDisabled();
    });

    it('requires a first name and does not call the API', async () => {
        const user = userEvent.setup();
        await renderProfile();

        await user.clear(screen.getByLabelText('First name'));
        await user.click(screen.getByRole('button', { name: 'Save changes' }));

        expect(await screen.findByText('Enter your first name.')).toBeInTheDocument();
        expect(screen.getByLabelText('First name')).toHaveAttribute('aria-invalid', 'true');
        expect(profileService.updateProfile).not.toHaveBeenCalled();
    });

    it('saves trimmed names and updates the signed-in user', async () => {
        const user = userEvent.setup();
        profileService.updateProfile.mockResolvedValue({ ...PROFILE, firstName: 'Jane', lastName: 'Smith' });
        const authStore = await renderProfile();

        await user.clear(screen.getByLabelText('First name'));
        await user.type(screen.getByLabelText('First name'), '  Jane ');
        await user.clear(screen.getByLabelText('Last name'));
        await user.type(screen.getByLabelText('Last name'), 'Smith');
        await user.click(screen.getByRole('button', { name: 'Save changes' }));

        expect(await screen.findByText('Profile saved.')).toBeInTheDocument();
        expect(profileService.updateProfile.mock.calls[0][0]).toEqual({ firstName: 'Jane', lastName: 'Smith' });
        expect(authStore.user.name).toBe('Jane Smith');
        expect(screen.getByRole('heading', { name: 'Jane Smith' })).toBeInTheDocument();
    });

    it('shows an error when saving fails', async () => {
        const user = userEvent.setup();
        profileService.updateProfile.mockRejectedValue({ response: { status: 500 } });
        await renderProfile();

        await user.type(screen.getByLabelText('Last name'), 'son');
        await user.click(screen.getByRole('button', { name: 'Save changes' }));

        expect(await screen.findByText("Couldn't save your profile. Please try again.")).toBeInTheDocument();
    });

    it('shows a retry action when the profile fails to load', async () => {
        profileService.fetchProfile.mockRejectedValue(new Error('network'));
        await renderWithPlugins(ProfileView, { route: '/profile' });

        await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent("Couldn't load your profile."));
        expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument();
    });
});

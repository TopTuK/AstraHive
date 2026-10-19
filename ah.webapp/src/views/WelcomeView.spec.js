import { screen } from '@testing-library/vue';
import { describe, expect, it } from 'vitest';
import { useAuthStore } from '@/stores/authStore';
import { renderWithPlugins } from '@/test/renderWithPlugins';
import WelcomeView from './WelcomeView.vue';

async function renderAs(user) {
    const { pinia } = await renderWithPlugins(WelcomeView, { route: '/welcome' });
    useAuthStore(pinia).user = user;
}

describe('WelcomeView', () => {
    it('greets the user by first name', async () => {
        await renderAs({ firstName: 'John', name: 'John Doe' });

        expect(await screen.findByRole('heading', { name: 'Welcome, John!' })).toBeInTheDocument();
    });

    it('falls back to the display name without a first name', async () => {
        await renderAs({ firstName: '', name: 'jdoe' });

        expect(await screen.findByRole('heading', { name: 'Welcome, jdoe!' })).toBeInTheDocument();
    });

    it('uses the anonymous greeting without a user', async () => {
        await renderAs(null);

        expect(await screen.findByRole('heading', { name: 'Welcome!' })).toBeInTheDocument();
    });

    it('announces that the project is under development', async () => {
        await renderAs(null);

        expect(screen.getByText('Under development')).toBeInTheDocument();
    });

    it('lists the upcoming sections', async () => {
        await renderAs(null);

        const items = screen.getAllByRole('listitem');
        expect(items).toHaveLength(4);
        for (const label of ['Clusters', 'Virtual machines', 'SSH sessions', 'Scheduled tasks']) {
            expect(screen.getByText(label)).toBeInTheDocument();
        }
    });
});

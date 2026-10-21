import { screen, within } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import * as certificateService from '@/services/certificateService';
import * as workspaceService from '@/services/workspaceService';
import { useAuthStore } from '@/stores/authStore';
import { renderWithPlugins } from '@/test/renderWithPlugins';
import WorkspaceMembersView from './WorkspaceMembersView.vue';

vi.mock('@/services/workspaceService', () => ({
    fetchWorkspaces: vi.fn(),
    fetchMembers: vi.fn(),
    addMember: vi.fn(),
    changeMemberRole: vi.fn(),
    removeMember: vi.fn(),
}));

vi.mock('@/services/certificateService', async (importOriginal) => ({
    ...(await importOriginal()),
    fetchCertificates: vi.fn(),
    issueCertificate: vi.fn(),
    revokeCertificate: vi.fn(),
}));

const ADMIN = { userId: 1, email: 'anna@example.com', firstName: 'Anna', lastName: 'Admin', avatarUrl: null, role: 'Administrator', isPending: false, addedAt: '2026-01-15T10:00:00Z' };
const VIEWER = { userId: 2, email: 'victor@example.com', firstName: 'Victor', lastName: '', avatarUrl: null, role: 'Viewer', isPending: false, addedAt: '2026-01-15T11:00:00Z' };

const membersAs = (yourRole, members = [ADMIN, VIEWER]) => ({ id: 10, name: "Anna's workspace", yourRole, members });

async function renderMembers(yourRole) {
    workspaceService.fetchWorkspaces.mockResolvedValue([{ id: 10, name: "Anna's workspace", role: yourRole, memberCount: 2 }]);
    workspaceService.fetchMembers.mockResolvedValue(membersAs(yourRole));
    const { pinia } = await renderWithPlugins(WorkspaceMembersView, { route: '/' });
    useAuthStore(pinia).user = { id: yourRole === 'Administrator' ? 1 : 2, email: 'x@example.com' };
    await screen.findByRole('table');
}

describe('WorkspaceMembersView', () => {
    beforeEach(() => {
        window.localStorage.clear();
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it('lists members with their roles', async () => {
        await renderMembers('Viewer');

        const rows = within(screen.getByRole('table')).getAllByRole('row');
        expect(rows[1]).toHaveTextContent('Anna Admin');
        expect(rows[1]).toHaveTextContent('Administrator');
        expect(rows[2]).toHaveTextContent('victor@example.com');
    });

    it('shows viewers a read-only list', async () => {
        await renderMembers('Viewer');

        expect(screen.queryByRole('form', { name: 'Add a member' })).not.toBeInTheDocument();
        expect(screen.queryByRole('combobox')).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: /Remove/ })).not.toBeInTheDocument();
    });

    it('marks members who have not signed in yet as pending', async () => {
        workspaceService.fetchMembers.mockResolvedValue(membersAs('Viewer', [ADMIN, { ...VIEWER, isPending: true }]));
        workspaceService.fetchWorkspaces.mockResolvedValue([{ id: 10, name: "Anna's workspace", role: 'Viewer', memberCount: 2 }]);
        await renderWithPlugins(WorkspaceMembersView, { route: '/' });

        expect(await screen.findByText('Pending')).toBeInTheDocument();
    });

    it('lets an administrator add a member by email', async () => {
        const user = userEvent.setup();
        const added = { ...VIEWER, userId: 3, email: 'new@example.com', firstName: '', isPending: true };
        workspaceService.addMember.mockResolvedValue(membersAs('Administrator', [ADMIN, VIEWER, added]));
        await renderMembers('Administrator');

        await user.type(screen.getByLabelText('Email'), ' new@example.com ');
        await user.selectOptions(screen.getByLabelText('Role'), 'Administrator');
        await user.click(screen.getByRole('button', { name: 'Add member' }));

        expect(workspaceService.addMember).toHaveBeenCalledWith(
            { workspaceId: 10, email: 'new@example.com', role: 'Administrator' },
            expect.anything(),
        );
        expect(await screen.findByText('new@example.com was added.')).toBeInTheDocument();
    });

    it('rejects an invalid email without calling the API', async () => {
        const user = userEvent.setup();
        await renderMembers('Administrator');

        await user.type(screen.getByLabelText('Email'), 'not-an-email');
        await user.click(screen.getByRole('button', { name: 'Add member' }));

        expect(screen.getByLabelText('Email')).toHaveAttribute('aria-invalid', 'true');
        expect(workspaceService.addMember).not.toHaveBeenCalled();
    });

    it('explains that the last administrator cannot be demoted', async () => {
        const user = userEvent.setup();
        workspaceService.changeMemberRole.mockRejectedValue({ response: { status: 409 } });
        await renderMembers('Administrator');

        await user.selectOptions(screen.getByLabelText('Role of Anna Admin'), 'Viewer');

        expect(await screen.findByText(/at least one administrator/)).toBeInTheDocument();
    });

    it('removes a member after confirmation', async () => {
        const user = userEvent.setup();
        vi.spyOn(window, 'confirm').mockReturnValue(true);
        workspaceService.removeMember.mockResolvedValue({});
        await renderMembers('Administrator');

        await user.click(screen.getByRole('button', { name: 'Remove Victor' }));

        expect(workspaceService.removeMember).toHaveBeenCalledWith({ workspaceId: 10, userId: 2 });
        expect(await screen.findByText('Victor was removed.')).toBeInTheDocument();
    });

    describe('in the main workspace', () => {
        const GLOBAL_ADMIN = { ...ADMIN, isGlobalAdministrator: true };
        const CERTIFICATE = {
            id: 7,
            userId: 2,
            thumbprint: 'A'.repeat(64),
            serialNumber: '01',
            notBefore: '2026-01-14T10:00:00Z',
            notAfter: '2099-01-14T10:00:00Z',
            issuedAt: '2026-01-15T10:00:00Z',
            revokedAt: null,
            canDownload: true,
        };

        async function renderMain({ canManageCertificates = true } = {}) {
            workspaceService.fetchWorkspaces.mockResolvedValue([{ id: 10, name: 'AstraHive', role: 'Administrator', memberCount: 2, isMain: true }]);
            workspaceService.fetchMembers.mockResolvedValue({
                id: 10,
                name: 'AstraHive',
                isMain: true,
                yourRole: 'Administrator',
                canManageCertificates,
                members: [GLOBAL_ADMIN, VIEWER],
            });
            await renderWithPlugins(WorkspaceMembersView, { route: '/' });
            await screen.findByRole('table');
        }

        it('keeps the global administrator role fixed', async () => {
            await renderMain();

            expect(screen.getByText('Global admin')).toBeInTheDocument();
            expect(screen.getByLabelText('Role of Anna Admin')).toBeDisabled();
            expect(screen.queryByRole('button', { name: 'Remove Anna Admin' })).not.toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Remove Victor' })).toBeInTheDocument();
        });

        it('hides certificates from other administrators', async () => {
            await renderMain({ canManageCertificates: false });

            expect(screen.queryByRole('button', { name: /Certificates of/ })).not.toBeInTheDocument();
            expect(screen.queryByLabelText(/Issue a client certificate/)).not.toBeInTheDocument();
        });

        it('issues a certificate and shows its password once', async () => {
            const user = userEvent.setup();
            certificateService.fetchCertificates.mockResolvedValue([]);
            certificateService.issueCertificate.mockResolvedValue({ certificate: CERTIFICATE, password: 'Secret-Password-42' });
            await renderMain();

            await user.click(screen.getByRole('button', { name: 'Certificates of Victor' }));
            await user.click(await screen.findByRole('button', { name: 'Issue certificate' }));

            expect(certificateService.issueCertificate).toHaveBeenCalledWith({ workspaceId: 10, userId: 2 }, expect.anything());
            expect(await screen.findByText('Secret-Password-42')).toBeInTheDocument();
            expect(screen.getAllByRole('link', { name: 'Download certificate' })[0])
                .toHaveAttribute('href', '/api/workspaces/10/members/2/certificates/7/download');
        });

        it('issues a certificate right after adding a member', async () => {
            const user = userEvent.setup();
            const added = { ...VIEWER, userId: 3, email: 'new@example.com', firstName: '', isPending: true };
            workspaceService.addMember.mockResolvedValue({ id: 10, name: 'AstraHive', isMain: true, yourRole: 'Administrator', canManageCertificates: true, members: [GLOBAL_ADMIN, VIEWER, added] });
            certificateService.fetchCertificates.mockResolvedValue([]);
            certificateService.issueCertificate.mockResolvedValue({ certificate: { ...CERTIFICATE, userId: 3 }, password: 'Fresh-Password-1' });
            await renderMain();

            await user.type(screen.getByLabelText('Email'), 'new@example.com');
            await user.click(screen.getByRole('button', { name: 'Add member' }));

            expect(await screen.findByText('Fresh-Password-1')).toBeInTheDocument();
            expect(certificateService.issueCertificate).toHaveBeenCalledWith({ workspaceId: 10, userId: 3 }, expect.anything());
        });
    });
});

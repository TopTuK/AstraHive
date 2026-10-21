import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { computed, toValue } from 'vue';
import * as workspaceService from '@/services/workspaceService';
import { useWorkspaceStore } from '@/stores/workspaceStore';

const WORKSPACES_KEY = ['workspaces'];
const membersKey = (workspaceId) => ['workspaces', workspaceId, 'members'];

export function useWorkspacesQuery() {
    return useQuery({
        queryKey: WORKSPACES_KEY,
        queryFn: workspaceService.fetchWorkspaces,
        staleTime: 5 * 60 * 1000,
    });
}

// The selected workspace, falling back to the first one when nothing (or a workspace the user
// no longer belongs to) is selected.
export function useCurrentWorkspace() {
    const store = useWorkspaceStore();
    const query = useWorkspacesQuery();

    const workspace = computed(() => {
        const workspaces = query.data.value ?? [];
        return workspaces.find((w) => w.id === store.selectedId) ?? workspaces[0] ?? null;
    });

    return { workspace, workspaces: query.data, isPending: query.isPending, isError: query.isError, refetch: query.refetch };
}

export function useWorkspaceMembersQuery(workspaceId) {
    return useQuery({
        queryKey: computed(() => membersKey(toValue(workspaceId))),
        queryFn: () => workspaceService.fetchMembers(toValue(workspaceId)),
        enabled: computed(() => toValue(workspaceId) != null),
    });
}

// Every member change answers with the workspace's members after the change; the list of workspaces
// is refetched because member counts and the requester's own role may have changed.
function useMembersMutation(mutationFn) {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn,
        onSuccess: (members, { workspaceId }) => {
            if (members) queryClient.setQueryData(membersKey(workspaceId), members);
            else queryClient.invalidateQueries({ queryKey: membersKey(workspaceId) });
            queryClient.invalidateQueries({ queryKey: WORKSPACES_KEY, exact: true });
        },
    });
}

export const useAddMemberMutation = () => useMembersMutation(workspaceService.addMember);

export const useChangeMemberRoleMutation = () => useMembersMutation(workspaceService.changeMemberRole);

export const useRemoveMemberMutation = () =>
    useMembersMutation((variables) => workspaceService.removeMember(variables).then(() => null));

// Only offered while registration is enabled; the server checks the flag again.
export function useCreateWorkspaceMutation() {
    const queryClient = useQueryClient();
    const store = useWorkspaceStore();

    return useMutation({
        mutationFn: workspaceService.createWorkspace,
        onSuccess: (workspace) => {
            queryClient.invalidateQueries({ queryKey: WORKSPACES_KEY, exact: true });
            store.select(workspace.id);
        },
    });
}

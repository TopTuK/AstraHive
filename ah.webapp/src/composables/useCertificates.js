import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import { computed, toValue } from 'vue';
import * as certificateService from '@/services/certificateService';

const certificatesKey = (workspaceId, userId) => ['workspaces', workspaceId, 'members', userId, 'certificates'];

export function useCertificatesQuery(workspaceId, userId) {
    return useQuery({
        queryKey: computed(() => certificatesKey(toValue(workspaceId), toValue(userId))),
        queryFn: () => certificateService.fetchCertificates({ workspaceId: toValue(workspaceId), userId: toValue(userId) }),
        enabled: computed(() => toValue(workspaceId) != null && toValue(userId) != null),
    });
}

export function useIssueCertificateMutation() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: certificateService.issueCertificate,
        onSuccess: (_, { workspaceId, userId }) =>
            queryClient.invalidateQueries({ queryKey: certificatesKey(workspaceId, userId) }),
    });
}

// Answers with the member's certificates after the revocation.
export function useRevokeCertificateMutation() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: certificateService.revokeCertificate,
        onSuccess: (certificates, { workspaceId, userId }) =>
            queryClient.setQueryData(certificatesKey(workspaceId, userId), certificates),
    });
}

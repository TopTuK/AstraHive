import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query';
import * as profileService from '@/services/profileService';
import { useAuthStore } from '@/stores/authStore';

const PROFILE_KEY = ['profile'];

export function useProfileQuery() {
    return useQuery({
        queryKey: PROFILE_KEY,
        queryFn: profileService.fetchProfile,
        staleTime: 5 * 60 * 1000,
    });
}

export function useUpdateProfileMutation() {
    const queryClient = useQueryClient();
    const authStore = useAuthStore();

    return useMutation({
        mutationFn: profileService.updateProfile,
        onSuccess: (profile) => {
            queryClient.setQueryData(PROFILE_KEY, profile);
            authStore.applyProfile(profile);
        },
    });
}

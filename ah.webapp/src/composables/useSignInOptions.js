import { useQuery } from '@tanstack/vue-query';
import * as authService from '@/services/authService';

// Feature flags change rarely and the server enforces them anyway, so a short cache is enough.
export function useSignInOptions() {
    return useQuery({
        queryKey: ['auth', 'options'],
        queryFn: authService.fetchSignInOptions,
        staleTime: 60 * 1000,
    });
}

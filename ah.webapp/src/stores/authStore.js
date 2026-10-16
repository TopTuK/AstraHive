import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

// Auth design is pending (likely owned by AH.IdentityService). Until then the
// app always runs unauthenticated; sign-in/session logic will be added here.
export const useAuthStore = defineStore('auth', () => {
    const user = ref(null);
    const isAuthenticated = computed(() => user.value !== null);

    return { user, isAuthenticated };
});

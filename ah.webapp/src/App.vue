<script setup>
import { TooltipProvider } from '@/components/ui/tooltip';
import AuthorizedLayout from '@/layout/AuthorizedLayout.vue';
import UnauthorizedLayout from '@/layout/UnauthorizedLayout.vue';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
</script>

<template>
  <TooltipProvider>
    <!-- Wait for the session probe (run by the router guard) so the wrong layout never flashes. -->
    <template v-if="authStore.initialized">
      <AuthorizedLayout v-if="authStore.isAuthenticated" />
      <UnauthorizedLayout v-else />
    </template>
  </TooltipProvider>
</template>

<script setup>
import { TooltipProvider } from '@/components/ui/tooltip';
import AuthorizedLayout from '@/layout/AuthorizedLayout.vue';
import UnauthorizedLayout from '@/layout/UnauthorizedLayout.vue';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
</script>

<template>
  <TooltipProvider>
    <AuthorizedLayout v-if="authStore.isAuthenticated" />
    <UnauthorizedLayout v-else />
  </TooltipProvider>
</template>

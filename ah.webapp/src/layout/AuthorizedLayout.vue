<script setup>
import { useLocalStorage } from '@vueuse/core';
import AppSidebar from './AppSidebar.vue';
import AuthorizedHeader from './AuthorizedHeader.vue';

const isSidebarCollapsed = useLocalStorage('astrahive-sidebar-collapsed', false);
</script>

<template>
  <div class="flex min-h-dvh bg-muted/50">
    <AppSidebar v-model:collapsed="isSidebarCollapsed" />
    <div class="flex min-w-0 flex-1 flex-col">
      <AuthorizedHeader />
      <main
        id="main-content"
        class="flex-1 p-4 sm:p-6"
      >
        <RouterView />
      </main>
    </div>
  </div>
</template>

<script setup>
import BaseLayout from './BaseLayout.vue';
import PublicFooter from './PublicFooter.vue';
import UnauthorizedHeader from './UnauthorizedHeader.vue';
</script>

<template>
  <BaseLayout>
    <template #header>
      <UnauthorizedHeader />
    </template>

    <RouterView />

    <template #footer>
      <PublicFooter />
    </template>
  </BaseLayout>
</template>

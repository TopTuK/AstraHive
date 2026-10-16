<script setup>
import AppLogo from '@/components/layout/AppLogo.vue';

const YEAR = new Date().getFullYear();
</script>

<template>
  <footer class="border-t bg-background">
    <div class="mx-auto flex max-w-6xl flex-col gap-4 px-4 py-8 sm:flex-row sm:items-center sm:justify-between sm:px-6">
      <div class="flex flex-col gap-1">
        <AppLogo />
        <p class="text-sm text-muted-foreground">
          {{ $t('footer.description') }}
        </p>
      </div>
      <p class="mono-label text-muted-foreground">
        &copy; {{ YEAR }} {{ $t('footer.rights') }}
      </p>
    </div>
  </footer>
</template>

<script setup>
import { ArrowLeft } from 'lucide-vue-next';
import { Button } from '@/components/ui/button';
</script>

<template>
  <section class="grid-bg flex flex-1 items-center justify-center px-4 py-16">
    <div class="flex max-w-md flex-col items-center gap-4 text-center">
      <img
        src="/brand/not-found.jpg"
        :alt="$t('notFound.imageAlt')"
        width="1024"
        height="1024"
        class="w-full max-w-sm rounded-md"
      />
      <span class="font-mono text-6xl font-medium text-muted-foreground">404</span>
      <h1 class="text-xl font-semibold">
        {{ $t('notFound.title') }}
      </h1>
      <p class="text-sm text-muted-foreground">
        {{ $t('notFound.text') }}
      </p>
      <Button
        as-child
        variant="outline"
      >
        <RouterLink :to="{ name: 'Home' }">
          <ArrowLeft aria-hidden="true" />
          {{ $t('notFound.back') }}
        </RouterLink>
      </Button>
    </div>
  </section>
</template>

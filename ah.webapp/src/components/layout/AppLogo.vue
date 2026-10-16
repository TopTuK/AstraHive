<script setup>
defineProps({
  showWordmark: { type: Boolean, default: true },
});
</script>

<template>
  <span class="inline-flex items-center gap-2.5">
    <img
      src="/brand/logo.jpg"
      :alt="showWordmark ? '' : $t('app.name')"
      width="512"
      height="512"
      class="size-8 shrink-0 rounded-md"
    />
    <span
      v-if="showWordmark"
      class="font-mono text-sm font-medium tracking-[0.2em] uppercase"
    >{{ $t('app.name') }}</span>
  </span>
</template>

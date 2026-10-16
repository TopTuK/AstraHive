<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
const imageSrc = computed(() => `/brand/${route.meta.image}.jpg`);
</script>

<template>
  <section class="panel flex flex-col items-center gap-4 px-6 py-10 text-center">
    <img
      :src="imageSrc"
      :alt="$t(`images.${route.meta.image}`)"
      width="1024"
      height="1024"
      class="w-full max-w-sm rounded-md"
    />
    <h2 class="font-semibold">
      {{ $t('placeholder.title') }}
    </h2>
    <p class="max-w-md text-sm text-muted-foreground">
      {{ $t('placeholder.text') }}
    </p>
  </section>
</template>

<script setup>
defineProps({
  index: { type: String, required: true },
  icon: { type: [Object, Function], required: true },
  title: { type: String, required: true },
  text: { type: String, required: true },
});
</script>

<template>
  <article class="group flex flex-col gap-4 bg-card p-6 transition-colors hover:bg-accent/40">
    <div class="flex items-center justify-between">
      <span class="flex size-9 items-center justify-center rounded-sm border bg-background text-primary">
        <component
          :is="icon"
          class="size-4"
          aria-hidden="true"
        />
      </span>
      <span class="mono-label text-muted-foreground">{{ index }}</span>
    </div>
    <h3 class="font-semibold">
      {{ title }}
    </h3>
    <p class="text-sm leading-relaxed text-muted-foreground">
      {{ text }}
    </p>
  </article>
</template>

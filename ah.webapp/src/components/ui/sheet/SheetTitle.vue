<script setup>
import { DialogTitle } from 'reka-ui';
import { cn } from '@/lib/utils';

const props = defineProps({
  class: { type: null, required: false },
});
</script>

<template>
  <DialogTitle
    data-slot="sheet-title"
    :class="cn('text-foreground font-semibold', props.class)"
  >
    <slot />
  </DialogTitle>
</template>

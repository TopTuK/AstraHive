<script setup>
import { DropdownMenuLabel } from 'reka-ui';
import { cn } from '@/lib/utils';

const props = defineProps({
  class: { type: null, required: false },
});
</script>

<template>
  <DropdownMenuLabel
    data-slot="dropdown-menu-label"
    :class="cn('px-2 py-1.5 text-sm font-medium', props.class)"
  >
    <slot />
  </DropdownMenuLabel>
</template>

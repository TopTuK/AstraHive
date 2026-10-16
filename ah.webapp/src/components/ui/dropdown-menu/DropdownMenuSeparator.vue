<script setup>
import { DropdownMenuSeparator } from 'reka-ui';
import { cn } from '@/lib/utils';

const props = defineProps({
  class: { type: null, required: false },
});
</script>

<template>
  <DropdownMenuSeparator
    data-slot="dropdown-menu-separator"
    :class="cn('bg-border -mx-1 my-1 h-px', props.class)"
  />
</template>

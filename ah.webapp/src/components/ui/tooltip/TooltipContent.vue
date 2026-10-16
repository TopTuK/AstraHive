<script setup>
import { TooltipContent, TooltipPortal } from 'reka-ui';
import { cn } from '@/lib/utils';

defineOptions({ inheritAttrs: false });

const props = defineProps({
  sideOffset: { type: Number, required: false, default: 4 },
  class: { type: null, required: false },
});
</script>

<template>
  <TooltipPortal>
    <TooltipContent
      data-slot="tooltip-content"
      v-bind="$attrs"
      :side-offset="sideOffset"
      :class="cn(
        'bg-foreground text-background animate-in fade-in-0 zoom-in-95 data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=closed]:zoom-out-95 z-50 w-fit rounded-md px-3 py-1.5 text-xs text-balance',
        props.class,
      )"
    >
      <slot />
    </TooltipContent>
  </TooltipPortal>
</template>

<script setup>
import { reactiveOmit } from '@vueuse/core';
import { Primitive } from 'reka-ui';
import { cn } from '@/lib/utils';
import { badgeVariants } from '.';

const props = defineProps({
  asChild: { type: Boolean, required: false },
  as: { type: null, required: false, default: 'span' },
  variant: { type: String, required: false },
  class: { type: null, required: false },
});

const delegatedProps = reactiveOmit(props, 'class');
</script>

<template>
  <Primitive
    data-slot="badge"
    v-bind="delegatedProps"
    :class="cn(badgeVariants({ variant }), props.class)"
  >
    <slot />
  </Primitive>
</template>

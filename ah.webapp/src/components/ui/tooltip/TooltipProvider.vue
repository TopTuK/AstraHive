<script setup>
import { TooltipProvider } from 'reka-ui';
</script>

<template>
  <TooltipProvider :delay-duration="0">
    <slot />
  </TooltipProvider>
</template>

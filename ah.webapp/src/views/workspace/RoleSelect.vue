<script setup>
import { cn } from '@/lib/utils';

// Values match the API's role names.
const ROLES = ['Administrator', 'Viewer'];

const props = defineProps({
  class: { type: null, required: false },
});

const role = defineModel({ type: String, required: true });
</script>

<template>
  <select
    v-model="role"
    :class="cn(
      'border-input flex h-9 rounded-md border bg-background px-2 py-1 text-sm shadow-xs outline-none transition-[color,box-shadow] focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50',
      props.class,
    )"
  >
    <option
      v-for="value in ROLES"
      :key="value"
      :value="value"
    >
      {{ $t(`roles.${value}`) }}
    </option>
  </select>
</template>

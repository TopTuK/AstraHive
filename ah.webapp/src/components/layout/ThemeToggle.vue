<script setup>
import { Moon, Sun } from 'lucide-vue-next';
import { Button } from '@/components/ui/button';
import { currentTheme, toggleTheme } from '@/theme';
</script>

<template>
  <Button
    variant="ghost"
    size="icon"
    :aria-label="$t('theme.toggle')"
    :title="$t('theme.toggle')"
    @click="toggleTheme"
  >
    <Sun v-if="currentTheme === 'dark'" aria-hidden="true" />
    <Moon v-else aria-hidden="true" />
  </Button>
</template>

<script setup>
import { useI18n } from 'vue-i18n';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { setLocale, SUPPORTED_LOCALES } from '@/i18n';

const { locale } = useI18n();
</script>

<template>
  <DropdownMenu>
    <DropdownMenuTrigger as-child>
      <Button
        variant="ghost"
        size="sm"
        class="font-mono text-xs uppercase"
        :aria-label="$t('locale.label')"
      >
        {{ locale }}
      </Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent align="end">
      <DropdownMenuItem
        v-for="code in SUPPORTED_LOCALES"
        :key="code"
        :class="code === locale && 'text-primary'"
        @select="setLocale(code)"
      >
        <span class="mono-label w-6 text-muted-foreground">{{ code }}</span>
        {{ $t(`locale.${code}`) }}
      </DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
</template>

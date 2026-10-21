<script setup>
import { PanelLeftClose, PanelLeftOpen } from 'lucide-vue-next';
import AppLogo from '@/components/layout/AppLogo.vue';
import { cn } from '@/lib/utils';
import SidebarNav from './SidebarNav.vue';
import WorkspaceSwitcher from './WorkspaceSwitcher.vue';

const isCollapsed = defineModel('collapsed', { type: Boolean, default: false });
</script>

<template>
  <aside
    :class="cn(
      'sticky top-0 hidden h-dvh shrink-0 flex-col border-r border-sidebar-border bg-sidebar text-sidebar-foreground transition-[width] duration-200 lg:flex',
      isCollapsed ? 'w-16' : 'w-60',
    )"
  >
    <RouterLink
      :to="{ name: 'Dashboard' }"
      :class="cn(
        'flex h-(--header-height) shrink-0 items-center border-b border-sidebar-border text-sidebar-accent-foreground focus-visible:bg-sidebar-accent focus-visible:outline-none',
        isCollapsed ? 'justify-center' : 'px-5',
      )"
    >
      <AppLogo :show-wordmark="!isCollapsed" />
    </RouterLink>

    <WorkspaceSwitcher v-if="!isCollapsed" />

    <div class="flex-1 overflow-y-auto">
      <SidebarNav :is-collapsed="isCollapsed" />
    </div>

    <button
      type="button"
      :class="cn(
        'flex h-10 shrink-0 items-center gap-3 border-t border-sidebar-border px-5 text-xs text-sidebar-muted transition-colors hover:text-sidebar-accent-foreground focus-visible:bg-sidebar-accent focus-visible:outline-none',
        isCollapsed && 'justify-center px-0',
      )"
      :aria-label="isCollapsed ? $t('sidebar.expand') : $t('sidebar.collapse')"
      :aria-expanded="!isCollapsed"
      @click="isCollapsed = !isCollapsed"
    >
      <PanelLeftOpen
        v-if="isCollapsed"
        class="size-4"
        aria-hidden="true"
      />
      <template v-else>
        <PanelLeftClose
          class="size-4"
          aria-hidden="true"
        />
        <span class="mono-label">{{ $t('sidebar.collapse') }}</span>
      </template>
    </button>
  </aside>
</template>

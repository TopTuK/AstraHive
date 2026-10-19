<script setup>
import {
  CalendarClock,
  Construction,
  LayoutDashboard,
  Network,
  Server,
  Settings,
  SquareTerminal,
} from 'lucide-vue-next';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';

defineProps({
  isCollapsed: { type: Boolean, default: false },
});

const emit = defineEmits(['navigate']);

const SECTIONS = [
  {
    label: 'sidebar.orchestration',
    items: [
      { name: 'Welcome', label: 'nav.welcome', icon: Construction },
      { name: 'Dashboard', label: 'nav.dashboard', icon: LayoutDashboard },
      { name: 'Clusters', label: 'nav.clusters', icon: Network },
      { name: 'Machines', label: 'nav.machines', icon: Server },
      { name: 'Sessions', label: 'nav.sessions', icon: SquareTerminal },
    ],
  },
  {
    label: 'sidebar.automation',
    items: [
      { name: 'Tasks', label: 'nav.tasks', icon: CalendarClock },
    ],
  },
  {
    label: 'sidebar.system',
    items: [
      { name: 'Settings', label: 'nav.settings', icon: Settings },
    ],
  },
];
</script>

<template>
  <nav
    class="flex flex-col gap-5 py-4"
    :aria-label="$t('nav.mainNavigation')"
  >
    <div
      v-for="section in SECTIONS"
      :key="section.label"
      class="flex flex-col gap-0.5"
    >
      <p
        v-if="!isCollapsed"
        class="mono-label px-5 pb-1.5 text-sidebar-muted"
      >
        {{ $t(section.label) }}
      </p>
      <div
        v-else
        class="mx-4 mb-1.5 h-px bg-sidebar-border"
        aria-hidden="true"
      />

      <Tooltip
        v-for="item in section.items"
        :key="item.name"
        :disabled="!isCollapsed"
      >
        <TooltipTrigger as-child>
          <RouterLink
            v-slot="{ href, navigate, isActive }"
            :to="{ name: item.name }"
            custom
          >
            <a
              :href="href"
              :aria-current="isActive ? 'page' : undefined"
              :aria-label="isCollapsed ? $t(item.label) : undefined"
              :class="cn(
                'relative flex h-9 items-center gap-3 px-5 text-sm text-sidebar-foreground transition-colors hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:bg-sidebar-accent focus-visible:outline-none',
                isCollapsed && 'justify-center px-0',
                isActive && 'bg-sidebar-accent text-sidebar-accent-foreground before:absolute before:inset-y-0 before:left-0 before:w-0.5 before:bg-sidebar-primary',
              )"
              @click="navigate($event); emit('navigate')"
            >
              <component
                :is="item.icon"
                class="size-4 shrink-0"
                aria-hidden="true"
              />
              <span v-if="!isCollapsed">{{ $t(item.label) }}</span>
            </a>
          </RouterLink>
        </TooltipTrigger>
        <TooltipContent side="right">
          {{ $t(item.label) }}
        </TooltipContent>
      </Tooltip>
    </div>
  </nav>
</template>

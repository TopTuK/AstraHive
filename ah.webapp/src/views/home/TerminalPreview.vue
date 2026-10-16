<script setup>
import { cn } from '@/lib/utils';

const LINES = [
  { kind: 'cmd', text: 'sudo apt-get update && sudo apt-get -y upgrade' },
  { kind: 'out', text: 'Reading package lists... Done' },
  { kind: 'out', text: '12 upgraded, 0 newly installed, 0 to remove.' },
  { kind: 'cmd', text: 'sudo systemctl restart nginx' },
  { kind: 'cmd', text: 'systemctl is-active nginx' },
  { kind: 'ok', text: 'active' },
];

const MACHINES = [
  { name: 'vm-web-01', host: '10.0.4.11', status: 'running' },
  { name: 'vm-web-02', host: '10.0.4.12', status: 'running' },
  { name: 'vm-db-01', host: '10.0.4.21', status: 'pending' },
  { name: 'vm-cache-01', host: '10.0.4.31', status: 'stopped' },
];

const STATUS_DOT = {
  running: 'bg-status-running',
  pending: 'bg-status-pending',
  stopped: 'bg-status-stopped',
  error: 'bg-status-error',
};
</script>

<template>
  <figure
    class="overflow-hidden rounded-md border border-terminal-border bg-terminal font-mono text-[13px] text-terminal-foreground shadow-2xl shadow-black/20"
    :aria-label="$t('home.terminal.label')"
  >
    <div class="flex items-center gap-2 border-b border-terminal-border px-4 py-2.5">
      <span class="size-2.5 rounded-full bg-terminal-muted/50" />
      <span class="size-2.5 rounded-full bg-terminal-muted/50" />
      <span class="size-2.5 rounded-full bg-terminal-muted/50" />
      <span class="ml-3 truncate text-xs text-terminal-muted">ssh ops@vm-web-01</span>
    </div>

    <div class="space-y-1 overflow-x-auto px-4 py-4 leading-relaxed">
      <p
        v-for="(line, index) in LINES"
        :key="index"
        class="whitespace-pre"
      >
        <template v-if="line.kind === 'cmd'">
          <span class="text-status-running">ops@vm-web-01</span><span class="text-terminal-muted">:~$ </span>{{ line.text }}
        </template>
        <span
          v-else
          :class="line.kind === 'ok' ? 'text-status-running' : 'text-terminal-muted'"
        >{{ line.text }}</span>
      </p>
      <p>
        <span class="text-status-running">ops@vm-web-01</span><span class="text-terminal-muted">:~$ </span><span class="inline-block h-4 w-2 translate-y-0.5 animate-pulse bg-terminal-foreground" />
      </p>
    </div>

    <div class="border-t border-terminal-border">
      <div class="flex items-center justify-between px-4 py-2 text-xs">
        <span class="text-terminal-foreground">cluster / eu-west-prod</span>
        <span class="text-terminal-muted">nightly-patch &middot; 0 2 * * *</span>
      </div>
      <ul class="divide-y divide-terminal-border border-t border-terminal-border">
        <li
          v-for="machine in MACHINES"
          :key="machine.name"
          class="grid grid-cols-[1fr_auto] items-center gap-4 px-4 py-1.5 text-xs sm:grid-cols-[1fr_8rem_6rem]"
        >
          <span>{{ machine.name }}</span>
          <span class="hidden text-terminal-muted sm:inline">{{ machine.host }}</span>
          <span class="flex items-center gap-2 text-terminal-muted">
            <span
              :class="cn('size-1.5 rounded-full', STATUS_DOT[machine.status])"
              aria-hidden="true"
            />
            {{ $t(`status.${machine.status}`) }}
          </span>
        </li>
      </ul>
    </div>
  </figure>
</template>

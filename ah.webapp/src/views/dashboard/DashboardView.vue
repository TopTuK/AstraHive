<script setup>
import { CalendarClock, Network, Server, SquareTerminal } from 'lucide-vue-next';

const STATS = [
  { key: 'clusters', icon: Network },
  { key: 'machines', icon: Server },
  { key: 'sessions', icon: SquareTerminal },
  { key: 'tasks', icon: CalendarClock },
];
</script>

<template>
  <div class="flex flex-col gap-6">
    <p class="text-sm text-muted-foreground">
      {{ $t('dashboard.subtitle') }}
    </p>

    <dl class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <div
        v-for="stat in STATS"
        :key="stat.key"
        class="panel flex items-center gap-4 p-4"
      >
        <span class="flex size-10 items-center justify-center rounded-sm bg-accent text-primary">
          <component
            :is="stat.icon"
            class="size-5"
            aria-hidden="true"
          />
        </span>
        <div class="flex flex-col">
          <dt class="mono-label text-muted-foreground">
            {{ $t(`dashboard.stats.${stat.key}`) }}
          </dt>
          <dd class="font-mono text-2xl font-medium">
            0
          </dd>
        </div>
      </div>
    </dl>

    <section class="panel flex flex-col items-center gap-4 px-6 py-10 text-center">
      <img
        src="/brand/dashboard.jpg"
        :alt="$t('dashboard.imageAlt')"
        width="1024"
        height="1024"
        class="w-full max-w-sm rounded-md"
      />
      <p class="max-w-md text-sm text-muted-foreground">
        {{ $t('dashboard.empty') }}
      </p>
    </section>
  </div>
</template>

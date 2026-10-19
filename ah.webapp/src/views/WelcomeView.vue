<script setup>
import { CalendarClock, Construction, Network, Server, SquareTerminal } from 'lucide-vue-next';
import { computed } from 'vue';
import { Badge } from '@/components/ui/badge';
import { useAuthStore } from '@/stores/authStore';

const UPCOMING = [
  { key: 'clusters', icon: Network },
  { key: 'machines', icon: Server },
  { key: 'sessions', icon: SquareTerminal },
  { key: 'tasks', icon: CalendarClock },
];

const authStore = useAuthStore();
const firstName = computed(() => authStore.user?.firstName || authStore.user?.name || '');
</script>

<template>
  <div class="flex flex-col gap-6">
    <section class="panel grid-bg flex flex-col items-center gap-5 px-6 py-12 text-center">
      <Badge variant="outline">
        <Construction aria-hidden="true" />
        {{ $t('welcome.badge') }}
      </Badge>
      <h2 class="text-2xl font-semibold sm:text-3xl">
        {{ firstName ? $t('welcome.greeting', { name: firstName }) : $t('welcome.greetingAnonymous') }}
      </h2>
      <p class="max-w-xl text-sm leading-relaxed text-muted-foreground sm:text-base">
        {{ $t('welcome.text') }}
      </p>
      <img
        src="/brand/dashboard.jpg"
        :alt="$t('dashboard.imageAlt')"
        width="1024"
        height="1024"
        class="w-full max-w-sm rounded-md"
      />
    </section>

    <section class="flex flex-col gap-3">
      <h3 class="mono-label text-muted-foreground">
        {{ $t('welcome.upcoming') }}
      </h3>
      <ul class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <li
          v-for="item in UPCOMING"
          :key="item.key"
          class="panel flex flex-col gap-3 p-4"
        >
          <div class="flex items-center justify-between">
            <span class="flex size-10 items-center justify-center rounded-sm bg-accent text-primary">
              <component
                :is="item.icon"
                class="size-5"
                aria-hidden="true"
              />
            </span>
            <Badge variant="secondary">
              {{ $t('welcome.soon') }}
            </Badge>
          </div>
          <p class="font-medium">
            {{ $t(`nav.${item.key}`) }}
          </p>
        </li>
      </ul>
    </section>
  </div>
</template>

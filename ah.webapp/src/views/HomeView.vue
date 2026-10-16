<script setup>
import { ArrowRight, CalendarClock, History, LogIn, Network, SquareTerminal } from 'lucide-vue-next';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import FeatureCard from './home/FeatureCard.vue';
import TerminalPreview from './home/TerminalPreview.vue';

const SPECS = [
  { label: 'home.specs.transport', value: 'home.specs.transportValue' },
  { label: 'home.specs.targets', value: 'home.specs.targetsValue' },
  { label: 'home.specs.grouping', value: 'home.specs.groupingValue' },
  { label: 'home.specs.scheduling', value: 'home.specs.schedulingValue' },
];

const FEATURES = [
  { key: 'ssh', icon: SquareTerminal },
  { key: 'clusters', icon: Network },
  { key: 'tasks', icon: CalendarClock },
  { key: 'audit', icon: History },
];

const STEPS = ['step1', 'step2', 'step3'];
</script>

<template>
  <div>
    <section class="grid-bg border-b">
      <div class="mx-auto grid max-w-6xl items-center gap-12 px-4 py-16 sm:px-6 lg:grid-cols-[1.05fr_1fr] lg:py-24">
        <div class="flex flex-col items-start gap-6">
          <div class="flex flex-wrap items-center gap-3">
            <span class="mono-label text-primary">{{ $t('home.hero.eyebrow') }}</span>
            <Badge
              variant="outline"
              class="mono-label gap-1.5 bg-background"
            >
              <span
                class="size-1.5 rounded-full bg-status-running"
                aria-hidden="true"
              />
              {{ $t('home.hero.status') }}
            </Badge>
          </div>
          <h1 class="text-4xl leading-[1.1] font-semibold tracking-tight text-balance sm:text-5xl">
            {{ $t('home.hero.title') }}
            <span class="text-muted-foreground">{{ $t('home.hero.titleAccent') }}</span>
          </h1>
          <p class="max-w-xl text-base leading-relaxed text-muted-foreground sm:text-lg">
            {{ $t('home.hero.subtitle') }}
          </p>
          <div class="flex flex-wrap gap-3">
            <Button
              as-child
              size="lg"
            >
              <RouterLink :to="{ name: 'Login' }">
                <LogIn aria-hidden="true" />
                {{ $t('home.hero.ctaPrimary') }}
              </RouterLink>
            </Button>
            <Button
              as-child
              size="lg"
              variant="outline"
            >
              <a href="#how-it-works">
                {{ $t('home.hero.ctaSecondary') }}
                <ArrowRight aria-hidden="true" />
              </a>
            </Button>
          </div>
        </div>

        <img
          src="/brand/home-hero.jpg"
          :alt="$t('home.hero.imageAlt')"
          width="1600"
          height="893"
          class="aspect-[4/3] min-w-0 w-full rounded-md border border-terminal-border object-cover object-right"
        />
      </div>

      <dl class="mx-auto grid max-w-6xl grid-cols-2 border-t lg:grid-cols-4">
        <div
          v-for="(spec, index) in SPECS"
          :key="spec.label"
          :class="[
            'flex flex-col gap-1 bg-background/70 px-4 py-4 sm:px-6',
            index % 2 === 1 && 'border-l',
            index >= 2 && 'border-t lg:border-t-0',
            index === 2 && 'lg:border-l',
          ]"
        >
          <dt class="mono-label text-muted-foreground">
            {{ $t(spec.label) }}
          </dt>
          <dd class="text-sm font-medium">
            {{ $t(spec.value) }}
          </dd>
        </div>
      </dl>
    </section>

    <section class="border-b">
      <div class="mx-auto max-w-3xl px-4 py-12 sm:px-6">
        <TerminalPreview />
      </div>
    </section>

    <section
      id="features"
      class="border-b"
    >
      <div class="mx-auto max-w-6xl px-4 py-16 sm:px-6 lg:py-20">
        <div class="mb-10 flex max-w-2xl flex-col gap-3">
          <span class="mono-label text-primary">{{ $t('home.features.label') }}</span>
          <h2 class="text-2xl font-semibold tracking-tight sm:text-3xl">
            {{ $t('home.features.title') }}
          </h2>
          <p class="text-muted-foreground">
            {{ $t('home.features.subtitle') }}
          </p>
        </div>
        <div class="grid gap-px overflow-hidden rounded-md border bg-border sm:grid-cols-2 lg:grid-cols-4">
          <FeatureCard
            v-for="(feature, index) in FEATURES"
            :key="feature.key"
            :index="String(index + 1).padStart(2, '0')"
            :icon="feature.icon"
            :title="$t(`home.features.${feature.key}.title`)"
            :text="$t(`home.features.${feature.key}.text`)"
          />
        </div>
      </div>
    </section>

    <section
      id="how-it-works"
      class="border-b bg-muted/40"
    >
      <div class="mx-auto max-w-6xl px-4 py-16 sm:px-6 lg:py-20">
        <div class="mb-10 flex flex-col gap-3">
          <span class="mono-label text-primary">{{ $t('home.how.label') }}</span>
          <h2 class="text-2xl font-semibold tracking-tight sm:text-3xl">
            {{ $t('home.how.title') }}
          </h2>
        </div>
        <ol class="grid gap-6 md:grid-cols-3">
          <li
            v-for="(step, index) in STEPS"
            :key="step"
            class="relative flex flex-col gap-3 border-t-2 border-foreground/80 pt-5"
          >
            <span class="font-mono text-sm text-muted-foreground">STEP_{{ String(index + 1).padStart(2, '0') }}</span>
            <h3 class="text-lg font-semibold">
              {{ $t(`home.how.${step}.title`) }}
            </h3>
            <p class="text-sm leading-relaxed text-muted-foreground">
              {{ $t(`home.how.${step}.text`) }}
            </p>
          </li>
        </ol>
      </div>
    </section>

    <section class="bg-sidebar text-sidebar-foreground">
      <div class="mx-auto flex max-w-6xl flex-col items-start gap-6 px-4 py-14 sm:px-6 md:flex-row md:items-center md:justify-between">
        <div class="flex flex-col gap-2">
          <h2 class="text-2xl font-semibold tracking-tight text-sidebar-accent-foreground">
            {{ $t('home.cta.title') }}
          </h2>
          <p class="text-sidebar-foreground">
            {{ $t('home.cta.text') }}
          </p>
        </div>
        <Button
          as-child
          size="lg"
        >
          <RouterLink :to="{ name: 'Login' }">
            {{ $t('home.cta.button') }}
            <ArrowRight aria-hidden="true" />
          </RouterLink>
        </Button>
      </div>
    </section>
  </div>
</template>

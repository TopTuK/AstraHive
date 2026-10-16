<script setup>
import { ArrowLeft, LockKeyhole } from 'lucide-vue-next';
import AppLogo from '@/components/layout/AppLogo.vue';
import { Button } from '@/components/ui/button';
</script>

<template>
  <section class="grid-bg flex flex-1 items-center justify-center px-4 py-16">
    <div class="panel grid w-full max-w-3xl overflow-hidden shadow-sm md:grid-cols-2">
      <img
        src="/brand/login.jpg"
        :alt="$t('login.imageAlt')"
        width="1024"
        height="1024"
        class="aspect-[4/3] w-full object-cover md:aspect-auto md:h-full"
      />
      <div>
        <div class="flex items-center justify-between border-b px-6 py-4">
          <AppLogo />
          <span class="mono-label text-muted-foreground">{{ $t('login.label') }}</span>
        </div>
        <div class="flex flex-col items-start gap-4 px-6 py-8">
          <span class="flex size-10 items-center justify-center rounded-sm border bg-muted text-muted-foreground">
            <LockKeyhole
              class="size-5"
              aria-hidden="true"
            />
          </span>
          <h1 class="text-xl font-semibold">
            {{ $t('login.title') }}
          </h1>
          <p class="text-sm leading-relaxed text-muted-foreground">
            {{ $t('login.text') }}
          </p>
          <Button
            as-child
            variant="outline"
          >
            <RouterLink :to="{ name: 'Home' }">
              <ArrowLeft aria-hidden="true" />
              {{ $t('login.back') }}
            </RouterLink>
          </Button>
        </div>
      </div>
    </div>
  </section>
</template>

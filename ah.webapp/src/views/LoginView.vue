<script setup>
import { ArrowLeft, CircleAlert, LockKeyhole } from 'lucide-vue-next';
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import AppLogo from '@/components/layout/AppLogo.vue';
import GoogleIcon from '@/components/icons/GoogleIcon.vue';
import YandexIcon from '@/components/icons/YandexIcon.vue';
import { Button } from '@/components/ui/button';
import { useAuthStore } from '@/stores/authStore';

const PROVIDERS = [
  { id: 'google', label: 'login.google', icon: GoogleIcon },
  { id: 'yandex', label: 'login.yandex', icon: YandexIcon },
];
const KNOWN_ERRORS = ['email_required', 'access_denied', 'sign_in_failed'];

const route = useRoute();
const authStore = useAuthStore();

const errorKey = computed(() => {
  const error = route.query.error;
  if (!error) return null;
  return KNOWN_ERRORS.includes(error) ? `login.errors.${error}` : 'login.errors.sign_in_failed';
});
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

          <p
            v-if="errorKey"
            role="alert"
            class="flex w-full items-start gap-2 rounded-sm border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive"
          >
            <CircleAlert
              class="mt-0.5 size-4 shrink-0"
              aria-hidden="true"
            />
            {{ $t(errorKey) }}
          </p>

          <div class="flex w-full flex-col gap-2">
            <Button
              v-for="provider in PROVIDERS"
              :key="provider.id"
              variant="outline"
              class="w-full justify-center"
              @click="authStore.loginWith(provider.id)"
            >
              <component
                :is="provider.icon"
                class="size-4"
                aria-hidden="true"
              />
              {{ $t(provider.label) }}
            </Button>
          </div>

          <Button
            as-child
            variant="ghost"
            size="sm"
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

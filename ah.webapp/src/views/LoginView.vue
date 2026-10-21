<script setup>
import { ArrowLeft, CircleAlert, KeyRound, LockKeyhole } from 'lucide-vue-next';
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import AppLogo from '@/components/layout/AppLogo.vue';
import GoogleIcon from '@/components/icons/GoogleIcon.vue';
import YandexIcon from '@/components/icons/YandexIcon.vue';
import { Button } from '@/components/ui/button';
import { useSignInOptions } from '@/composables/useSignInOptions';
import { useAuthStore } from '@/stores/authStore';

const EXTERNAL_PROVIDERS = [
  { id: 'google', label: 'login.google', icon: GoogleIcon },
  { id: 'yandex', label: 'login.yandex', icon: YandexIcon },
];
// Hops to the certificate login port, where the browser asks which client certificate to use.
const CERTIFICATE_PROVIDER = { id: 'certificate', label: 'login.certificate', icon: KeyRound };
const KNOWN_ERRORS = [
  'email_required',
  'email_invalid',
  'email_unverified',
  'access_denied',
  'sign_in_failed',
  'certificate_required',
  'certificate_invalid',
  'certificate_unavailable',
  'certificate_unknown',
  'external_login_disabled',
  'registration_disabled',
];

const route = useRoute();
const authStore = useAuthStore();
const { data: options } = useSignInOptions();

// Google/Yandex appear only when the server allows them (ah.external_login); the certificate is always
// offered, also while the options load or if they fail, because it is the default way in.
const externalProviders = computed(() =>
  EXTERNAL_PROVIDERS.filter((provider) => options.value?.externalProviders?.includes(provider.id)),
);
const providers = computed(() => [...externalProviders.value, CERTIFICATE_PROVIDER]);

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
            {{ externalProviders.length ? $t('login.textExternal') : $t('login.text') }}
            <template v-if="externalProviders.length && options?.registration">
              {{ $t('login.registration') }}
            </template>
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
              v-for="provider in providers"
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

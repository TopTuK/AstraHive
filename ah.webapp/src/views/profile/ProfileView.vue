<script setup>
import { LoaderCircle, UserRound } from 'lucide-vue-next';
import { computed, reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useProfileQuery, useUpdateProfileMutation } from '@/composables/useProfile';

// Mirrors User.NameMaxLength on the backend.
const NAME_MAX_LENGTH = 100;

const { t, locale } = useI18n();
const { data: profile, isPending, isError, refetch } = useProfileQuery();
const { mutateAsync: saveProfile, isPending: isSaving } = useUpdateProfileMutation();

const form = reactive({ firstName: '', lastName: '' });
const isSubmitted = ref(false);
const status = ref(null);

const resetForm = () => {
  form.firstName = profile.value?.firstName ?? '';
  form.lastName = profile.value?.lastName ?? '';
  isSubmitted.value = false;
};

// Refill the form whenever fresh server data arrives (first load or after a save).
watch(profile, resetForm, { immediate: true });

const errors = computed(() => {
  const result = {};
  const firstName = form.firstName.trim();
  const lastName = form.lastName.trim();
  if (!firstName) result.firstName = t('profile.errors.firstNameRequired');
  else if (firstName.length > NAME_MAX_LENGTH) result.firstName = t('profile.errors.tooLong', { max: NAME_MAX_LENGTH });
  if (lastName.length > NAME_MAX_LENGTH) result.lastName = t('profile.errors.tooLong', { max: NAME_MAX_LENGTH });
  return result;
});

const visibleErrors = computed(() => (isSubmitted.value ? errors.value : {}));

const isDirty = computed(() =>
  form.firstName.trim() !== (profile.value?.firstName ?? '')
  || form.lastName.trim() !== (profile.value?.lastName ?? ''));

const displayName = computed(() =>
  `${profile.value?.firstName ?? ''} ${profile.value?.lastName ?? ''}`.trim() || profile.value?.email);

const memberSince = computed(() =>
  profile.value?.createdAt
    ? new Intl.DateTimeFormat(locale.value, { dateStyle: 'long' }).format(new Date(profile.value.createdAt))
    : '');

async function handleSubmit() {
  isSubmitted.value = true;
  status.value = null;
  if (Object.keys(errors.value).length > 0) return;

  try {
    await saveProfile({ firstName: form.firstName.trim(), lastName: form.lastName.trim() });
    status.value = { type: 'success', message: t('profile.saved') };
  } catch (error) {
    const detail = error?.response?.status === 400 ? error.response.data?.detail : null;
    status.value = { type: 'error', message: detail || t('profile.errors.saveFailed') };
  }
}

function handleReset() {
  resetForm();
  status.value = null;
}
</script>

<template>
  <div class="flex max-w-3xl flex-col gap-6">
    <p class="text-sm text-muted-foreground">
      {{ $t('profile.description') }}
    </p>

    <div
      v-if="isPending"
      class="panel flex items-center gap-3 p-6 text-sm text-muted-foreground"
      role="status"
    >
      <LoaderCircle
        class="size-4 animate-spin"
        aria-hidden="true"
      />
      {{ $t('profile.loading') }}
    </div>

    <div
      v-else-if="isError"
      class="panel flex flex-col items-start gap-3 p-6"
      role="alert"
    >
      <p class="text-sm text-destructive">
        {{ $t('profile.errors.loadFailed') }}
      </p>
      <Button
        variant="outline"
        size="sm"
        @click="refetch()"
      >
        {{ $t('profile.errors.retry') }}
      </Button>
    </div>

    <template v-else>
      <section class="panel flex items-center gap-4 p-6">
        <img
          v-if="profile.avatarUrl"
          :src="profile.avatarUrl"
          alt=""
          referrerpolicy="no-referrer"
          class="size-16 rounded-full object-cover"
        />
        <span
          v-else
          class="flex size-16 items-center justify-center rounded-full bg-accent text-primary"
        >
          <UserRound
            class="size-8"
            aria-hidden="true"
          />
        </span>
        <div class="flex min-w-0 flex-col gap-1">
          <h2 class="truncate text-lg font-semibold">
            {{ displayName }}
          </h2>
          <dl
            v-if="memberSince"
            class="flex gap-1 text-sm text-muted-foreground"
          >
            <dt>{{ $t('profile.memberSince') }}:</dt>
            <dd>{{ memberSince }}</dd>
          </dl>
        </div>
      </section>

      <form
        class="panel flex flex-col gap-5 p-6"
        novalidate
        :aria-label="$t('profile.title')"
        @submit.prevent="handleSubmit"
      >
        <h3 class="mono-label text-muted-foreground">
          {{ $t('profile.title') }}
        </h3>

        <div class="grid gap-5 sm:grid-cols-2">
          <div class="flex flex-col gap-2">
            <Label for="profile-first-name">{{ $t('profile.firstName') }}</Label>
            <Input
              id="profile-first-name"
              v-model="form.firstName"
              autocomplete="given-name"
              required
              :aria-invalid="visibleErrors.firstName ? 'true' : undefined"
              :aria-describedby="visibleErrors.firstName ? 'profile-first-name-error' : undefined"
            />
            <p
              v-if="visibleErrors.firstName"
              id="profile-first-name-error"
              class="text-sm text-destructive"
            >
              {{ visibleErrors.firstName }}
            </p>
          </div>

          <div class="flex flex-col gap-2">
            <Label for="profile-last-name">{{ $t('profile.lastName') }}</Label>
            <Input
              id="profile-last-name"
              v-model="form.lastName"
              autocomplete="family-name"
              :aria-invalid="visibleErrors.lastName ? 'true' : undefined"
              :aria-describedby="visibleErrors.lastName ? 'profile-last-name-error' : undefined"
            />
            <p
              v-if="visibleErrors.lastName"
              id="profile-last-name-error"
              class="text-sm text-destructive"
            >
              {{ visibleErrors.lastName }}
            </p>
          </div>
        </div>

        <div class="flex flex-col gap-2">
          <Label for="profile-email">{{ $t('profile.email') }}</Label>
          <Input
            id="profile-email"
            :model-value="profile.email"
            type="email"
            readonly
            disabled
            aria-describedby="profile-email-hint"
          />
          <p
            id="profile-email-hint"
            class="text-xs text-muted-foreground"
          >
            {{ $t('profile.emailHint') }}
          </p>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <Button
            type="submit"
            :disabled="isSaving || !isDirty"
          >
            <LoaderCircle
              v-if="isSaving"
              class="animate-spin"
              aria-hidden="true"
            />
            {{ isSaving ? $t('profile.saving') : $t('profile.save') }}
          </Button>
          <Button
            type="button"
            variant="outline"
            :disabled="isSaving || !isDirty"
            @click="handleReset"
          >
            {{ $t('profile.reset') }}
          </Button>
          <p
            class="text-sm"
            :class="status?.type === 'error' ? 'text-destructive' : 'text-status-running'"
            aria-live="polite"
          >
            {{ status?.message }}
          </p>
        </div>
      </form>
    </template>
  </div>
</template>

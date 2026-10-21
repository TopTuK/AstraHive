<script setup>
import { KeyRound, LoaderCircle, Trash2, UserPlus, UserRound } from 'lucide-vue-next';
import { computed, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  useAddMemberMutation,
  useChangeMemberRoleMutation,
  useCurrentWorkspace,
  useRemoveMemberMutation,
  useWorkspaceMembersQuery,
} from '@/composables/useWorkspaces';
import { useAuthStore } from '@/stores/authStore';
import MemberCertificatesSheet from './MemberCertificatesSheet.vue';
import RoleSelect from './RoleSelect.vue';

// Mirrors User.EmailMaxLength on the backend; the server validates the address too.
const EMAIL_MAX_LENGTH = 320;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+$/;

const { t } = useI18n();
const authStore = useAuthStore();
const { workspace, isPending: isWorkspacesPending, isError: isWorkspacesError, refetch: refetchWorkspaces } = useCurrentWorkspace();
const workspaceId = computed(() => workspace.value?.id ?? null);
const { data: details, isPending: isMembersPending, isError: isMembersError, refetch: refetchMembers } = useWorkspaceMembersQuery(workspaceId);

const { mutateAsync: addMember, isPending: isAdding } = useAddMemberMutation();
const { mutateAsync: changeRole, isPending: isChangingRole } = useChangeMemberRoleMutation();
const { mutateAsync: removeMember, isPending: isRemoving } = useRemoveMemberMutation();

const form = reactive({ email: '', role: 'Viewer', issueCertificate: true });
const isSubmitted = ref(false);
const status = ref(null);

const isLoading = computed(() => isWorkspacesPending.value || (workspaceId.value !== null && isMembersPending.value));
const isLoadError = computed(() => isWorkspacesError.value || isMembersError.value);
const isAdministrator = computed(() => details.value?.yourRole === 'Administrator');
const isBusy = computed(() => isAdding.value || isChangingRole.value || isRemoving.value);
// Only the Global Administrator, in the main workspace, issues client certificates.
const canManageCertificates = computed(() => details.value?.canManageCertificates === true);

const certificateMember = ref(null);
const isCertificatesOpen = ref(false);
const issueOnOpen = ref(false);

function openCertificates(member, { issue = false } = {}) {
  certificateMember.value = member;
  issueOnOpen.value = issue;
  isCertificatesOpen.value = true;
}

const emailError = computed(() => {
  if (!isSubmitted.value) return null;
  const email = form.email.trim();
  if (!email || email.length > EMAIL_MAX_LENGTH || !EMAIL_PATTERN.test(email)) return t('workspace.errors.emailInvalid');
  return null;
});

const displayName = (member) => `${member.firstName} ${member.lastName}`.trim() || member.email;

// The server's ProblemDetails text is English-only, so the message comes from the status and the action.
function errorMessage(error, conflictKey) {
  switch (error?.response?.status) {
    case 400: return t('workspace.errors.emailInvalid');
    case 403: return t('workspace.errors.forbidden');
    case 404: return t('workspace.errors.notFound');
    case 409: return t(conflictKey);
    default: return t('workspace.errors.actionFailed');
  }
}

async function handleAdd() {
  isSubmitted.value = true;
  status.value = null;
  if (emailError.value) return;

  const email = form.email.trim();
  try {
    const updated = await addMember({ workspaceId: workspaceId.value, email, role: form.role });
    status.value = { type: 'success', message: t('workspace.added', { email }) };
    form.email = '';
    isSubmitted.value = false;
    const added = updated?.members.find((member) => member.email === email.toLowerCase());
    if (canManageCertificates.value && form.issueCertificate && added) openCertificates(added, { issue: true });
  } catch (error) {
    status.value = { type: 'error', message: errorMessage(error, 'workspace.errors.alreadyMember') };
  }
}

async function handleRoleChange(member, role) {
  if (role === member.role) return;
  status.value = null;
  try {
    await changeRole({ workspaceId: workspaceId.value, userId: member.userId, role });
    status.value = { type: 'success', message: t('workspace.roleChanged', { name: displayName(member) }) };
  } catch (error) {
    status.value = { type: 'error', message: errorMessage(error, 'workspace.errors.lastAdministrator') };
  }
}

async function handleRemove(member) {
  if (!window.confirm(t('workspace.removeConfirm', { name: displayName(member) }))) return;
  status.value = null;
  try {
    await removeMember({ workspaceId: workspaceId.value, userId: member.userId });
    status.value = { type: 'success', message: t('workspace.removed', { name: displayName(member) }) };
  } catch (error) {
    status.value = { type: 'error', message: errorMessage(error, 'workspace.errors.lastAdministrator') };
  }
}

function retry() {
  refetchWorkspaces();
  if (workspaceId.value !== null) refetchMembers();
}
</script>

<template>
  <div class="flex max-w-4xl flex-col gap-6">
    <div
      v-if="isLoading"
      class="panel flex items-center gap-3 p-6 text-sm text-muted-foreground"
      role="status"
    >
      <LoaderCircle
        class="size-4 animate-spin"
        aria-hidden="true"
      />
      {{ $t('workspace.loading') }}
    </div>

    <div
      v-else-if="isLoadError"
      class="panel flex flex-col items-start gap-3 p-6"
      role="alert"
    >
      <p class="text-sm text-destructive">
        {{ $t('workspace.errors.loadFailed') }}
      </p>
      <Button
        variant="outline"
        size="sm"
        @click="retry"
      >
        {{ $t('workspace.errors.retry') }}
      </Button>
    </div>

    <p
      v-else-if="!workspace"
      class="panel p-6 text-sm text-muted-foreground"
    >
      {{ $t('workspace.none') }}
    </p>

    <template v-else-if="details">
      <div class="flex flex-col gap-1">
        <h2 class="text-lg font-semibold">
          {{ details.name }}
        </h2>
        <p class="text-sm text-muted-foreground">
          {{ isAdministrator ? $t('workspace.descriptionAdmin') : $t('workspace.descriptionViewer') }}
        </p>
      </div>

      <form
        v-if="isAdministrator"
        class="panel flex flex-col gap-4 p-6"
        novalidate
        :aria-label="$t('workspace.add.title')"
        @submit.prevent="handleAdd"
      >
        <h3 class="mono-label text-muted-foreground">
          {{ $t('workspace.add.title') }}
        </h3>
        <div class="flex flex-col gap-3 sm:flex-row sm:items-end">
          <div class="flex flex-1 flex-col gap-2">
            <Label for="member-email">{{ $t('workspace.add.email') }}</Label>
            <Input
              id="member-email"
              v-model="form.email"
              type="email"
              autocomplete="off"
              :aria-invalid="emailError ? 'true' : undefined"
              aria-describedby="member-email-hint"
            />
          </div>
          <div class="flex flex-col gap-2">
            <Label for="member-role">{{ $t('workspace.add.role') }}</Label>
            <RoleSelect
              id="member-role"
              v-model="form.role"
            />
          </div>
          <Button
            type="submit"
            :disabled="isBusy"
          >
            <LoaderCircle
              v-if="isAdding"
              class="animate-spin"
              aria-hidden="true"
            />
            <UserPlus
              v-else
              aria-hidden="true"
            />
            {{ $t('workspace.add.submit') }}
          </Button>
        </div>
        <label
          v-if="canManageCertificates"
          class="flex items-center gap-2 text-sm"
        >
          <input
            v-model="form.issueCertificate"
            type="checkbox"
            class="size-4 accent-primary"
          />
          {{ $t('workspace.add.issueCertificate') }}
        </label>
        <p
          id="member-email-hint"
          class="text-xs"
          :class="emailError ? 'text-destructive' : 'text-muted-foreground'"
        >
          {{ emailError ?? $t('workspace.add.hint') }}
        </p>
      </form>

      <p
        class="min-h-5 text-sm"
        :class="status?.type === 'error' ? 'text-destructive' : 'text-status-running'"
        aria-live="polite"
      >
        {{ status?.message }}
      </p>

      <section class="panel overflow-x-auto">
        <table class="w-full text-sm">
          <caption class="sr-only">
            {{ $t('workspace.membersCaption', { name: details.name }) }}
          </caption>
          <thead class="border-b text-left">
            <tr>
              <th
                scope="col"
                class="mono-label px-6 py-3 font-normal text-muted-foreground"
              >
                {{ $t('workspace.columns.member') }}
              </th>
              <th
                scope="col"
                class="mono-label px-6 py-3 font-normal text-muted-foreground"
              >
                {{ $t('workspace.columns.role') }}
              </th>
              <th
                v-if="isAdministrator"
                scope="col"
                class="px-6 py-3"
              >
                <span class="sr-only">{{ $t('workspace.columns.actions') }}</span>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="member in details.members"
              :key="member.userId"
              class="border-b last:border-b-0"
            >
              <td class="px-6 py-3">
                <div class="flex items-center gap-3">
                  <img
                    v-if="member.avatarUrl"
                    :src="member.avatarUrl"
                    alt=""
                    referrerpolicy="no-referrer"
                    class="size-8 shrink-0 rounded-full object-cover"
                  />
                  <span
                    v-else
                    class="flex size-8 shrink-0 items-center justify-center rounded-full bg-accent text-primary"
                  >
                    <UserRound
                      class="size-4"
                      aria-hidden="true"
                    />
                  </span>
                  <div class="flex min-w-0 flex-col">
                    <span class="flex items-center gap-2 truncate font-medium">
                      {{ displayName(member) }}
                      <span
                        v-if="member.userId === authStore.user?.id"
                        class="text-xs font-normal text-muted-foreground"
                      >{{ $t('workspace.you') }}</span>
                      <Badge
                        v-if="member.isGlobalAdministrator"
                        variant="secondary"
                        :title="$t('workspace.globalAdminHint')"
                      >
                        {{ $t('workspace.globalAdmin') }}
                      </Badge>
                      <Badge
                        v-if="member.isPending"
                        variant="outline"
                        :title="$t('workspace.pendingHint')"
                      >
                        {{ $t('workspace.pending') }}
                      </Badge>
                    </span>
                    <span class="truncate text-xs text-muted-foreground">{{ member.email }}</span>
                  </div>
                </div>
              </td>
              <td class="px-6 py-3">
                <RoleSelect
                  v-if="isAdministrator"
                  :model-value="member.role"
                  :aria-label="$t('workspace.roleFor', { name: displayName(member) })"
                  :disabled="isBusy || member.isGlobalAdministrator"
                  @update:model-value="handleRoleChange(member, $event)"
                />
                <span v-else>{{ $t(`roles.${member.role}`) }}</span>
              </td>
              <td
                v-if="isAdministrator"
                class="px-6 py-3 text-right"
              >
                <Button
                  v-if="canManageCertificates"
                  variant="ghost"
                  size="icon"
                  :aria-label="$t('workspace.certificates', { name: displayName(member) })"
                  @click="openCertificates(member)"
                >
                  <KeyRound aria-hidden="true" />
                </Button>
                <Button
                  v-if="!member.isGlobalAdministrator"
                  variant="ghost"
                  size="icon"
                  :disabled="isBusy"
                  :aria-label="$t('workspace.remove', { name: displayName(member) })"
                  @click="handleRemove(member)"
                >
                  <Trash2 aria-hidden="true" />
                </Button>
              </td>
            </tr>
          </tbody>
        </table>
      </section>

      <MemberCertificatesSheet
        v-if="canManageCertificates"
        v-model:open="isCertificatesOpen"
        :workspace-id="details.id"
        :member="certificateMember"
        :issue-on-open="issueOnOpen"
      />
    </template>
  </div>
</template>

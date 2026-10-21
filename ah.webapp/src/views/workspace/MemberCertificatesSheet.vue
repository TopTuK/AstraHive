<script setup>
import { Check, Copy, Download, KeyRound, LoaderCircle, ShieldOff } from 'lucide-vue-next';
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import {
  useCertificatesQuery,
  useIssueCertificateMutation,
  useRevokeCertificateMutation,
} from '@/composables/useCertificates';
import { downloadUrl } from '@/services/certificateService';

// The Global Administrator's view of one member's client certificates. The PFX password exists only in the
// issue response, so it is shown here once, right after issuing.
const props = defineProps({
  workspaceId: { type: Number, required: true },
  member: { type: Object, default: null },
  // Issue a certificate as soon as the sheet opens (adding a member with "issue a certificate").
  issueOnOpen: { type: Boolean, default: false },
});

const open = defineModel('open', { type: Boolean, default: false });

const { t, locale } = useI18n();
const userId = computed(() => props.member?.userId ?? null);
const { data: certificates, isPending, isError } = useCertificatesQuery(() => props.workspaceId, userId);
const { mutateAsync: issueCertificate, isPending: isIssuing } = useIssueCertificateMutation();
const { mutateAsync: revokeCertificate, isPending: isRevoking } = useRevokeCertificateMutation();

const issued = ref(null);
const isCopied = ref(false);
const status = ref(null);

const displayName = computed(() => {
  const member = props.member;
  if (!member) return '';
  return `${member.firstName} ${member.lastName}`.trim() || member.email;
});

watch(open, (isOpen) => {
  issued.value = null;
  isCopied.value = false;
  status.value = null;
  if (isOpen && props.issueOnOpen) handleIssue();
});

function errorMessage(error) {
  switch (error?.response?.status) {
    case 403: return t('certificates.errors.forbidden');
    case 404: return t('certificates.errors.notFound');
    case 409: return t('certificates.errors.conflict');
    default: return t('certificates.errors.actionFailed');
  }
}

async function handleIssue() {
  status.value = null;
  isCopied.value = false;
  try {
    issued.value = await issueCertificate({ workspaceId: props.workspaceId, userId: userId.value });
  } catch (error) {
    status.value = errorMessage(error);
  }
}

async function handleRevoke(certificate) {
  if (!window.confirm(t('certificates.revokeConfirm', { name: displayName.value }))) return;
  status.value = null;
  try {
    await revokeCertificate({ workspaceId: props.workspaceId, userId: userId.value, certificateId: certificate.id });
    if (issued.value?.certificate.id === certificate.id) issued.value = null;
  } catch (error) {
    status.value = errorMessage(error);
  }
}

async function handleCopy() {
  try {
    await navigator.clipboard.writeText(issued.value.password);
    isCopied.value = true;
  } catch {
    // Clipboard access can be denied; the password stays on screen to copy by hand.
  }
}

const fileUrl = (certificate) =>
  downloadUrl({ workspaceId: props.workspaceId, userId: userId.value, certificateId: certificate.id });

const formatDate = (value) => new Date(value).toLocaleDateString(locale.value);

const shortThumbprint = (thumbprint) => `${thumbprint.slice(0, 8)}...${thumbprint.slice(-8)}`;

const certificateState = (certificate) => {
  if (certificate.revokedAt) return 'revoked';
  return new Date(certificate.notAfter) <= new Date() ? 'expired' : 'active';
};
</script>

<template>
  <Sheet v-model:open="open">
    <SheetContent
      class="w-full overflow-y-auto sm:max-w-md"
      :close-label="$t('certificates.close')"
    >
      <SheetHeader class="border-b">
        <SheetTitle>{{ $t('certificates.title', { name: displayName }) }}</SheetTitle>
        <SheetDescription class="text-sm text-muted-foreground">
          {{ $t('certificates.description') }}
        </SheetDescription>
      </SheetHeader>

      <div class="flex flex-col gap-4 px-4 pb-6">
        <Button
          class="self-start"
          :disabled="isIssuing || isRevoking"
          @click="handleIssue"
        >
          <LoaderCircle
            v-if="isIssuing"
            class="animate-spin"
            aria-hidden="true"
          />
          <KeyRound
            v-else
            aria-hidden="true"
          />
          {{ $t('certificates.issue') }}
        </Button>

        <div
          v-if="issued"
          class="flex flex-col gap-3 rounded-sm border border-status-running/40 bg-status-running/10 p-4"
          role="status"
        >
          <p class="text-sm font-medium">
            {{ $t('certificates.issued') }}
          </p>
          <div class="flex items-center gap-2">
            <code
              class="flex-1 rounded-sm border bg-background px-2 py-1 font-mono text-sm break-all"
              :aria-label="$t('certificates.password')"
            >{{ issued.password }}</code>
            <Button
              variant="outline"
              size="icon"
              :aria-label="isCopied ? $t('certificates.copied') : $t('certificates.copy')"
              @click="handleCopy"
            >
              <Check
                v-if="isCopied"
                aria-hidden="true"
              />
              <Copy
                v-else
                aria-hidden="true"
              />
            </Button>
          </div>
          <p class="text-xs text-muted-foreground">
            {{ $t('certificates.passwordOnce') }}
          </p>
          <Button
            as="a"
            variant="outline"
            size="sm"
            class="self-start"
            :href="fileUrl(issued.certificate)"
            download
          >
            <Download aria-hidden="true" />
            {{ $t('certificates.download') }}
          </Button>
        </div>

        <p
          v-if="status"
          class="text-sm text-destructive"
          role="alert"
        >
          {{ status }}
        </p>

        <h3 class="mono-label text-muted-foreground">
          {{ $t('certificates.listTitle') }}
        </h3>
        <p
          v-if="isPending"
          class="flex items-center gap-2 text-sm text-muted-foreground"
          role="status"
        >
          <LoaderCircle
            class="size-4 animate-spin"
            aria-hidden="true"
          />
          {{ $t('certificates.loading') }}
        </p>
        <p
          v-else-if="isError"
          class="text-sm text-destructive"
        >
          {{ $t('certificates.errors.loadFailed') }}
        </p>
        <p
          v-else-if="!certificates?.length"
          class="text-sm text-muted-foreground"
        >
          {{ $t('certificates.empty') }}
        </p>
        <ul
          v-else
          class="flex flex-col divide-y rounded-sm border"
        >
          <li
            v-for="certificate in certificates"
            :key="certificate.id"
            class="flex items-center gap-3 px-3 py-2"
          >
            <div class="flex min-w-0 flex-1 flex-col gap-0.5">
              <span class="flex items-center gap-2">
                <code
                  class="truncate font-mono text-xs"
                  :title="certificate.thumbprint"
                >{{ shortThumbprint(certificate.thumbprint) }}</code>
                <Badge :variant="certificateState(certificate) === 'active' ? 'secondary' : 'outline'">
                  {{ $t(`certificates.state.${certificateState(certificate)}`) }}
                </Badge>
              </span>
              <span class="text-xs text-muted-foreground">
                {{ $t('certificates.validUntil', { date: formatDate(certificate.notAfter) }) }}
              </span>
            </div>
            <Button
              v-if="certificate.canDownload"
              as="a"
              variant="ghost"
              size="icon"
              :href="fileUrl(certificate)"
              download
              :aria-label="$t('certificates.download')"
            >
              <Download aria-hidden="true" />
            </Button>
            <Button
              v-if="!certificate.revokedAt"
              variant="ghost"
              size="icon"
              :disabled="isIssuing || isRevoking"
              :aria-label="$t('certificates.revoke')"
              @click="handleRevoke(certificate)"
            >
              <ShieldOff aria-hidden="true" />
            </Button>
          </li>
        </ul>
      </div>
    </SheetContent>
  </Sheet>
</template>

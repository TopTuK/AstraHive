<script setup>
import { Check, ChevronsUpDown, Plus, Users } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useSignInOptions } from '@/composables/useSignInOptions';
import { useCreateWorkspaceMutation, useCurrentWorkspace } from '@/composables/useWorkspaces';
import { useWorkspaceStore } from '@/stores/workspaceStore';

const emit = defineEmits(['navigate']);

const store = useWorkspaceStore();
const { workspace, workspaces } = useCurrentWorkspace();
const { data: options } = useSignInOptions();
const { mutate: createWorkspace } = useCreateWorkspaceMutation();
const { t } = useI18n();

// Creating workspaces belongs to self-service registration (ah.registration_enabled); the server checks it too.
function handleCreate() {
  const name = window.prompt(t('workspace.create.prompt'))?.trim();
  if (!name) return;
  createWorkspace({ name }, { onError: () => window.alert(t('workspace.create.failed')) });
}
</script>

<template>
  <DropdownMenu v-if="workspace">
    <DropdownMenuTrigger
      class="flex w-full items-center gap-2 border-b border-sidebar-border px-5 py-3 text-left transition-colors hover:bg-sidebar-accent focus-visible:bg-sidebar-accent focus-visible:outline-none"
      :aria-label="$t('workspace.switch', { name: workspace.name })"
    >
      <span class="flex min-w-0 flex-1 flex-col">
        <span class="mono-label text-sidebar-muted">{{ $t('workspace.label') }}</span>
        <span class="truncate text-sm text-sidebar-accent-foreground">{{ workspace.name }}</span>
      </span>
      <ChevronsUpDown
        class="size-4 shrink-0 text-sidebar-muted"
        aria-hidden="true"
      />
    </DropdownMenuTrigger>
    <DropdownMenuContent
      align="start"
      class="w-60"
    >
      <DropdownMenuLabel class="mono-label text-muted-foreground">
        {{ $t('workspace.label') }}
      </DropdownMenuLabel>
      <DropdownMenuItem
        v-for="item in workspaces"
        :key="item.id"
        @select="store.select(item.id)"
      >
        <Check
          :class="item.id === workspace.id ? 'opacity-100' : 'opacity-0'"
          aria-hidden="true"
        />
        <span class="min-w-0 flex-1 truncate">{{ item.name }}</span>
        <span class="text-xs text-muted-foreground">{{ $t(`roles.${item.role}`) }}</span>
      </DropdownMenuItem>
      <DropdownMenuItem
        v-if="options?.registration"
        @select="handleCreate"
      >
        <Plus aria-hidden="true" />
        {{ $t('workspace.create.action') }}
      </DropdownMenuItem>
      <DropdownMenuSeparator />
      <DropdownMenuItem
        as-child
        @select="emit('navigate')"
      >
        <RouterLink :to="{ name: 'WorkspaceMembers' }">
          <Users aria-hidden="true" />
          {{ $t('workspace.manageMembers') }}
        </RouterLink>
      </DropdownMenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
</template>

<script setup>
import { LogOut, Menu, UserRound } from 'lucide-vue-next';
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppLogo from '@/components/layout/AppLogo.vue';
import LocaleSwitcher from '@/components/layout/LocaleSwitcher.vue';
import ThemeToggle from '@/components/layout/ThemeToggle.vue';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Separator } from '@/components/ui/separator';
import { Sheet, SheetContent, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { useAuthStore } from '@/stores/authStore';
import SidebarNav from './SidebarNav.vue';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const isMobileNavOpen = ref(false);

async function signOut() {
  try {
    await authStore.logout();
  } finally {
    await router.push({ name: 'Home' });
  }
}
</script>

<template>
  <header class="sticky top-0 z-30 flex h-(--header-height) shrink-0 items-center gap-3 border-b bg-background px-4 sm:px-6">
    <Sheet v-model:open="isMobileNavOpen">
      <SheetTrigger as-child>
        <Button
          variant="ghost"
          size="icon"
          class="-ml-2 lg:hidden"
          :aria-label="$t('nav.openMenu')"
        >
          <Menu aria-hidden="true" />
        </Button>
      </SheetTrigger>
      <SheetContent
        side="left"
        class="w-64 border-sidebar-border bg-sidebar p-0 text-sidebar-foreground"
        :close-label="$t('nav.closeMenu')"
        :aria-describedby="undefined"
      >
        <SheetTitle class="flex h-(--header-height) items-center border-b border-sidebar-border px-5 text-sidebar-accent-foreground">
          <AppLogo />
        </SheetTitle>
        <SidebarNav @navigate="isMobileNavOpen = false" />
      </SheetContent>
    </Sheet>

    <div class="flex min-w-0 items-center gap-2 text-sm">
      <span class="mono-label hidden text-muted-foreground sm:inline">{{ $t('app.name') }}</span>
      <span
        class="hidden text-muted-foreground sm:inline"
        aria-hidden="true"
      >/</span>
      <h1 class="truncate font-medium">
        {{ route.meta.title ? $t(route.meta.title) : '' }}
      </h1>
    </div>

    <div class="ml-auto flex items-center gap-1">
      <LocaleSwitcher />
      <ThemeToggle />
      <Separator
        orientation="vertical"
        class="mx-2 h-5!"
      />
      <DropdownMenu>
        <DropdownMenuTrigger as-child>
          <Button
            variant="ghost"
            size="sm"
            :aria-label="$t('header.userMenu')"
          >
            <img
              v-if="authStore.user?.avatarUrl"
              :src="authStore.user.avatarUrl"
              alt=""
              referrerpolicy="no-referrer"
              class="size-5 rounded-full object-cover"
            />
            <UserRound
              v-else
              aria-hidden="true"
            />
            <span class="hidden max-w-32 truncate sm:inline">{{ authStore.user?.name }}</span>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="end"
          class="w-48"
        >
          <DropdownMenuLabel class="mono-label text-muted-foreground">
            {{ $t('header.account') }}
          </DropdownMenuLabel>
          <DropdownMenuItem @select="router.push({ name: 'Profile' })">
            <UserRound aria-hidden="true" />
            {{ $t('header.profile') }}
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem @select="signOut">
            <LogOut aria-hidden="true" />
            {{ $t('header.signOut') }}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  </header>
</template>

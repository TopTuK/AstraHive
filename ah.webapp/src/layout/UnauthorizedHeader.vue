<script setup>
import { LogIn, Menu } from 'lucide-vue-next';
import AppLogo from '@/components/layout/AppLogo.vue';
import LocaleSwitcher from '@/components/layout/LocaleSwitcher.vue';
import ThemeToggle from '@/components/layout/ThemeToggle.vue';
import { Button } from '@/components/ui/button';
import {
  Sheet,
  SheetClose,
  SheetContent,
  SheetTitle,
  SheetTrigger,
} from '@/components/ui/sheet';

const NAV_LINKS = [
  { hash: '#features', label: 'nav.features' },
  { hash: '#how-it-works', label: 'nav.howItWorks' },
];
</script>

<template>
  <header class="sticky top-0 z-40 h-(--header-height) border-b bg-background/85 backdrop-blur">
    <div class="mx-auto flex h-full max-w-6xl items-center gap-6 px-4 sm:px-6">
      <RouterLink
        :to="{ name: 'Home' }"
        class="rounded-sm focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none"
      >
        <AppLogo />
      </RouterLink>

      <nav
        class="hidden items-center gap-1 md:flex"
        :aria-label="$t('nav.mainNavigation')"
      >
        <RouterLink
          v-for="link in NAV_LINKS"
          :key="link.hash"
          :to="{ name: 'Home', hash: link.hash }"
          class="rounded-sm px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
        >
          {{ $t(link.label) }}
        </RouterLink>
      </nav>

      <div class="ml-auto flex items-center gap-1">
        <LocaleSwitcher />
        <ThemeToggle />
        <Button
          as-child
          size="sm"
          class="ml-2 hidden sm:inline-flex"
        >
          <RouterLink :to="{ name: 'Login' }">
            <LogIn aria-hidden="true" />
            {{ $t('nav.signIn') }}
          </RouterLink>
        </Button>

        <Sheet>
          <SheetTrigger as-child>
            <Button
              variant="ghost"
              size="icon"
              class="md:hidden"
              :aria-label="$t('nav.openMenu')"
            >
              <Menu aria-hidden="true" />
            </Button>
          </SheetTrigger>
          <SheetContent
            side="right"
            :close-label="$t('nav.closeMenu')"
            :aria-describedby="undefined"
          >
            <SheetTitle class="border-b px-4 py-4">
              <AppLogo />
            </SheetTitle>
            <nav
              class="flex flex-col px-2"
              :aria-label="$t('nav.mainNavigation')"
            >
              <SheetClose
                v-for="link in NAV_LINKS"
                :key="link.hash"
                as-child
              >
                <RouterLink
                  :to="{ name: 'Home', hash: link.hash }"
                  class="rounded-sm px-3 py-2.5 text-sm hover:bg-accent"
                >
                  {{ $t(link.label) }}
                </RouterLink>
              </SheetClose>
            </nav>
            <div class="mt-auto border-t p-4">
              <SheetClose as-child>
                <Button
                  as-child
                  class="w-full"
                >
                  <RouterLink :to="{ name: 'Login' }">
                    <LogIn aria-hidden="true" />
                    {{ $t('nav.signIn') }}
                  </RouterLink>
                </Button>
              </SheetClose>
            </div>
          </SheetContent>
        </Sheet>
      </div>
    </div>
  </header>
</template>

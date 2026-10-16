<template>
  <div class="flex min-h-dvh flex-col">
    <slot name="header" />
    <main
      id="main-content"
      class="flex flex-1 flex-col"
    >
      <slot />
    </main>
    <slot name="footer" />
  </div>
</template>

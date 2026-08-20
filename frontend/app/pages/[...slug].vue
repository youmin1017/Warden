<script setup lang="ts">
// A real matched route for any unknown path — without this, Nuxt falls back to its
// internal error render, which re-runs global middleware a second time within the same
// SSR request and can race the single-use refresh token (see stores/auth.ts).
</script>

<template>
  <UCard class="w-full max-w-sm text-center">
    <h1 class="text-lg font-semibold mb-2">
      Page not found
    </h1>
    <p class="text-muted mb-4">
      The page you're looking for doesn't exist.
    </p>
    <UButton to="/">
      Go home
    </UButton>
  </UCard>
</template>

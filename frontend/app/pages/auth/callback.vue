<script setup lang="ts">
definePageMeta({ layout: 'default', middleware: 'oidc-callback' })
</script>

<template>
  <UCard class="w-full max-w-sm text-center">
    <p class="text-muted">
      登入中,請稍候…
    </p>
  </UCard>
</template>

<script setup lang="ts">
definePageMeta({ layout: 'default' })
</script>

<template>
  <div class="min-h-screen flex flex-col items-center justify-center gap-4">
    <h1 class="text-2xl font-semibold">
      沒有權限
    </h1>
    <p class="text-muted">
      你的帳號沒有存取此頁面的權限。
    </p>
    <UButton to="/admin">
      回到首頁
    </UButton>
  </div>
</template>

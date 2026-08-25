<script setup lang="ts">
import type { NuxtError } from '#app'

const props = defineProps({
  error: Object as () => NuxtError
})

const messages: Record<number, { statusMessage: string; message: string }> = {
  403: { statusMessage: '沒有權限', message: '你的帳號沒有存取此頁面的權限。' },
  404: { statusMessage: '找不到頁面', message: '你要找的頁面不存在。' },
  500: { statusMessage: '伺服器錯誤', message: '伺服器發生錯誤，請稍後再試。' }
}

const displayError = computed(() => {
  const status = props.error?.status ?? 500
  return {
    statusCode: status,
    ...(messages[status] ?? {
      statusMessage: props.error?.statusText,
      message: props.error?.message
    })
  }
})
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-default">
    <UError :error="displayError" redirect="/admin" :clear="{ label: '回到首頁' }" />
  </div>
</template>

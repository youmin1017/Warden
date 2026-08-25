<script setup lang="ts">
import type { OidcProviderDto } from '~/types/api'

definePageMeta({ layout: 'default' })

const config = useRuntimeConfig()
const route = useRoute()

const { data: providers, error: providersError } = await useFetch<OidcProviderDto[]>('/api/auth/oidc/providers', {
  baseURL: config.public.apiBase
})

const errorMessage = computed(() => {
  if (route.query.error === 'oidc_failed') {
    return '登入失敗,請再試一次或聯絡管理員。'
  }
  if (providersError.value) {
    return '無法連線至伺服器,請稍後再試或聯絡管理員。'
  }
  return ''
})

function challengeUrl(providerName: string) {
  return `${config.public.apiBase}/api/auth/oidc/${providerName}/challenge`
}
</script>

<template>
  <UCard class="w-full max-w-sm">
    <template #header>
      <h1 class="text-lg font-semibold">
        Sign in to Warden
      </h1>
    </template>

    <div class="flex flex-col gap-4">
      <UAlert v-if="errorMessage" color="error" variant="subtle" :title="errorMessage" />

      <!-- Real top-level navigation, not a client-side route — the IdP requires a full page load. -->
      <UButton
        v-for="provider in providers ?? []"
        :key="provider.name"
        :to="challengeUrl(provider.name)"
        external
        block
      >
        使用 {{ provider.displayName }} 登入
      </UButton>

      <p v-if="!providersError && !providers?.length" class="text-muted text-sm text-center">
        尚未設定任何登入方式。
      </p>
    </div>
  </UCard>
</template>

<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

const auth = useAuthStore()
// const toast = useToast()

const email = ref('admin@warden.local')
const password = ref('')
const isLoading = ref(false)
const errorMessage = ref('')

async function submit() {
  errorMessage.value = ''
  isLoading.value = true
  try {
    await auth.login(email.value, password.value)
    await navigateTo('/admin')
  } catch {
    errorMessage.value = 'Invalid email or password.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <UCard class="w-full max-w-sm">
    <template #header>
      <h1 class="text-lg font-semibold">Sign in to Warden</h1>
    </template>

    <form class="flex flex-col gap-4" @submit.prevent="submit">
      <UFormField label="Email">
        <UInput v-model="email" type="email" autocomplete="username" class="w-full" />
      </UFormField>

      <UFormField label="Password">
        <UInput v-model="password" type="password" autocomplete="current-password" class="w-full" />
      </UFormField>

      <UAlert v-if="errorMessage" color="error" variant="subtle" :title="errorMessage" />

      <UButton type="submit" block :loading="isLoading">Sign in</UButton>
    </form>
  </UCard>
</template>

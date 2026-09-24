<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

definePageMeta({ layout: 'user' })

const auth = useAuthStore()
</script>

<template>
  <div class="flex flex-col gap-6">
    <div>
      <h1 class="text-2xl font-semibold">
        Welcome, {{ auth.user?.displayName }}
      </h1>
      <p class="text-muted mt-1">
        This is the Warden user portal.
      </p>
    </div>

    <div class="grid gap-4 sm:grid-cols-2">
      <UCard>
        <template #header>
          <div class="flex items-center gap-2 font-medium">
            <UIcon name="i-lucide-user" />
            Your account
          </div>
        </template>
        <p class="text-sm text-muted">
          Signed in as {{ auth.user?.email }}.
        </p>
        <template #footer>
          <UButton to="/profile" variant="link" trailing-icon="i-lucide-arrow-right" class="px-0">
            View profile
          </UButton>
        </template>
      </UCard>

      <UCard>
        <template #header>
          <div class="flex items-center gap-2 font-medium">
            <UIcon name="i-lucide-shield" />
            Roles
          </div>
        </template>
        <div v-if="auth.user?.roles.length" class="flex flex-wrap gap-2">
          <UBadge v-for="role in auth.user.roles" :key="role" variant="subtle">
            {{ role }}
          </UBadge>
        </div>
        <p v-else class="text-sm text-muted">
          You have no roles assigned yet.
        </p>
      </UCard>
    </div>
  </div>
</template>

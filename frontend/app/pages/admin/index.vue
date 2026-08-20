<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

definePageMeta({ layout: 'admin' })

const auth = useAuthStore()
</script>

<template>
  <div>
    <h1 class="text-2xl font-semibold mb-4">
      Dashboard
    </h1>
    <UCard>
      <p>Welcome, {{ auth.user?.displayName }}.</p>
      <p class="text-muted mt-2">
        Roles: {{ auth.user?.roles.join(', ') }}
      </p>
      <p class="text-muted mt-1">
        Permissions: {{ auth.user?.permissions.join(', ') }}
      </p>
    </UCard>
  </div>
</template>

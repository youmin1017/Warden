<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

definePageMeta({ layout: 'user' })

const auth = useAuthStore()
</script>

<template>
  <div class="flex flex-col gap-6">
    <h1 class="text-2xl font-semibold">
      Profile
    </h1>

    <UCard>
      <dl class="grid gap-4 sm:grid-cols-[10rem_1fr]">
        <dt class="text-sm text-muted">Display name</dt>
        <dd>{{ auth.user?.displayName }}</dd>

        <dt class="text-sm text-muted">Email</dt>
        <dd>{{ auth.user?.email }}</dd>

        <dt class="text-sm text-muted">Roles</dt>
        <dd>
          <div v-if="auth.user?.roles.length" class="flex flex-wrap gap-2">
            <UBadge v-for="role in auth.user.roles" :key="role" variant="subtle">
              {{ role }}
            </UBadge>
          </div>
          <span v-else class="text-muted">None</span>
        </dd>

        <dt class="text-sm text-muted">Permissions</dt>
        <dd>
          <div v-if="auth.user?.permissions.length" class="flex flex-wrap gap-2">
            <UBadge v-for="perm in auth.user.permissions" :key="perm" color="neutral" variant="outline" class="font-mono">
              {{ perm }}
            </UBadge>
          </div>
          <span v-else class="text-muted">None</span>
        </dd>
      </dl>
    </UCard>
  </div>
</template>

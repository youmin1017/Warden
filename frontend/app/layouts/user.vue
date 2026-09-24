<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

const auth = useAuthStore()

interface NavItem {
  label: string
  path: string
  icon: string
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Home', path: '/', icon: 'i-lucide-house' },
  { label: 'Profile', path: '/profile', icon: 'i-lucide-user' }
] as const

// Mirrors the permission-gated entries in layouts/admin.vue — only surface the console link
// to users who would actually see something there beyond the bare dashboard.
const ADMIN_PERMISSIONS = ['user.read', 'role.read', 'apikey.read']

const canAccessAdmin = computed(() => ADMIN_PERMISSIONS.some((p) => auth.hasPermission(p)))

async function handleLogout() {
  await auth.logout()
  await navigateTo('/login')
}
</script>

<template>
  <div class="flex min-h-screen flex-col bg-default">
    <header class="border-b border-default">
      <div class="mx-auto flex h-14 items-center gap-2 px-4">
        <NuxtLink to="/" class="mr-4 text-lg font-bold">Warden</NuxtLink>

        <UButton
          v-for="item in NAV_ITEMS"
          :key="item.path"
          :to="item.path"
          :icon="item.icon"
          variant="ghost"
          color="neutral"
          active-variant="soft"
        >
          {{ item.label }}
        </UButton>

        <div class="flex-1" />

        <UButton v-if="canAccessAdmin" to="/admin" icon="i-lucide-shield-check" variant="ghost" color="neutral">
          Admin
        </UButton>
        <span class="hidden px-2 text-sm text-muted sm:inline">
          {{ auth.user?.displayName }}
        </span>
        <UButton icon="i-lucide-log-out" variant="soft" color="neutral" @click="handleLogout">Log out</UButton>
      </div>
    </header>

    <main class="mx-auto w-full flex-1 p-6">
      <slot />
    </main>
  </div>
</template>

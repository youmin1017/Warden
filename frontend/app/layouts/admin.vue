<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'

const auth = useAuthStore()

interface NavItem {
  label: string
  path: string
  icon: string
  permission?: string
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', path: '/admin', icon: 'i-lucide-layout-dashboard' },
  { label: 'Users', path: '/admin/users', icon: 'i-lucide-users', permission: 'user.read' },
  { label: 'Roles', path: '/admin/roles', icon: 'i-lucide-shield', permission: 'role.read' },
  { label: 'API Keys', path: '/admin/api-keys', icon: 'i-lucide-key', permission: 'apikey.read' }
] as const

const menu = computed(() => NAV_ITEMS.filter((item) => !item.permission || auth.hasPermission(item.permission)))

async function handleLogout() {
  await auth.logout()
  await navigateTo('/login')
}
</script>

<template>
  <div class="flex min-h-screen bg-default">
    <aside class="flex w-60 shrink-0 flex-col gap-1 border-r border-default p-4">
      <div class="mb-4 px-2 text-lg font-bold">Warden Admin</div>
      <UButton
        v-for="item in menu"
        :key="item.path"
        :to="item.path"
        :icon="item.icon"
        variant="ghost"
        color="neutral"
        block
        class="justify-start"
      >
        {{ item.label }}
      </UButton>

      <div class="flex-1" />

      <UButton
        to="/"
        icon="i-lucide-arrow-left"
        variant="ghost"
        color="neutral"
        block
        class="justify-start"
      >
        Back to site
      </UButton>
      <div class="mb-2 px-2 text-sm text-muted">
        {{ auth.user?.displayName }}
      </div>
      <UButton icon="i-lucide-log-out" variant="soft" color="neutral" block @click="handleLogout">Log out</UButton>
    </aside>

    <main class="flex-1 overflow-x-auto p-6">
      <slot />
    </main>
  </div>
</template>

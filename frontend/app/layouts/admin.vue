<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'
import type { MenuItemDto } from '~/types/api'

const { request } = useApi()
const auth = useAuthStore()

const { data: menu } = await useAsyncData('admin-menu-for-current-user', () =>
  request<MenuItemDto[]>('/api/admin/menu/for-current-user')
)

async function handleLogout() {
  await auth.logout()
  await navigateTo('/login')
}
</script>

<template>
  <div class="flex min-h-screen bg-default">
    <aside class="w-60 shrink-0 border-r border-default p-4 flex flex-col gap-1">
      <div class="font-bold text-lg mb-4 px-2">
        Warden Admin
      </div>

      <UButton
        v-for="item in menu ?? []"
        :key="item.id"
        :to="item.path ?? undefined"
        :icon="item.icon ?? undefined"
        variant="ghost"
        color="neutral"
        block
        class="justify-start"
      >
        {{ item.label }}
      </UButton>

      <div class="flex-1" />

      <div class="px-2 text-sm text-muted mb-2">
        {{ auth.user?.displayName }}
      </div>
      <UButton icon="i-lucide-log-out" variant="soft" color="neutral" block @click="handleLogout">
        Log out
      </UButton>
    </aside>

    <main class="flex-1 p-6 overflow-x-auto">
      <slot />
    </main>
  </div>
</template>

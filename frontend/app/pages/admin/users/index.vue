<script setup lang="ts">
import { h, resolveComponent } from 'vue'
import type { TableColumn } from '@nuxt/ui'
import type { RoleDto, UserDto } from '~/types/api'

definePageMeta({ layout: 'admin' })

const { request } = useApi()
const toast = useToast()

const { data: users, refresh: refreshUsers, status } = await useAsyncData('admin-users', () =>
  request<UserDto[]>('/api/admin/users')
)
const { data: roles } = await useAsyncData('admin-roles-for-users', () =>
  request<RoleDto[]>('/api/admin/roles')
)

const roleOptions = computed(() => (roles.value ?? []).map(r => r.name))

const isModalOpen = ref(false)
const editingUser = ref<UserDto | null>(null)

const form = reactive({
  email: '',
  displayName: '',
  password: '',
  isActive: true,
  roleNames: [] as string[]
})

function openCreate() {
  editingUser.value = null
  form.email = ''
  form.displayName = ''
  form.password = ''
  form.isActive = true
  form.roleNames = []
  isModalOpen.value = true
}

function openEdit(user: UserDto) {
  editingUser.value = user
  form.email = user.email
  form.displayName = user.displayName
  form.password = ''
  form.isActive = user.isActive
  form.roleNames = [...user.roles]
  isModalOpen.value = true
}

async function submit() {
  try {
    if (editingUser.value) {
      await request(`/api/admin/users/${editingUser.value.id}`, {
        method: 'PUT',
        body: { displayName: form.displayName, isActive: form.isActive, roleNames: form.roleNames }
      })
    } else {
      await request('/api/admin/users', {
        method: 'POST',
        body: { email: form.email, displayName: form.displayName, password: form.password, roleNames: form.roleNames }
      })
    }
    isModalOpen.value = false
    await refreshUsers()
    toast.add({ title: 'Saved', color: 'success' })
  } catch (error: unknown) {
    const message = (error as { data?: { message?: string } })?.data?.message ?? 'Something went wrong.'
    toast.add({ title: 'Error', description: message, color: 'error' })
  }
}

async function remove(user: UserDto) {
  await request(`/api/admin/users/${user.id}`, { method: 'DELETE' })
  await refreshUsers()
  toast.add({ title: 'User deleted', color: 'success' })
}

const UButton = resolveComponent('UButton')
const UBadge = resolveComponent('UBadge')

const columns: TableColumn<UserDto>[] = [
  { accessorKey: 'email', header: 'Email' },
  { accessorKey: 'displayName', header: 'Name' },
  {
    accessorKey: 'roles',
    header: 'Roles',
    cell: ({ row }) => h('div', { class: 'flex gap-1 flex-wrap' }, row.original.roles.map(name => h(UBadge, { key: name, color: 'neutral', variant: 'subtle' }, () => name)))
  },
  {
    accessorKey: 'isActive',
    header: 'Active',
    cell: ({ row }) => h(UBadge, { color: row.original.isActive ? 'success' : 'neutral', variant: 'subtle' }, () => row.original.isActive ? 'Active' : 'Inactive')
  },
  {
    id: 'actions',
    header: '',
    cell: ({ row }) => h('div', { class: 'flex gap-2 justify-end' }, [
      h(UButton, { size: 'xs', color: 'neutral', variant: 'ghost', icon: 'i-lucide-pencil', onClick: () => openEdit(row.original) }),
      h(UButton, { size: 'xs', color: 'error', variant: 'ghost', icon: 'i-lucide-trash-2', onClick: () => remove(row.original) })
    ])
  }
]
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4">
      <h1 class="text-2xl font-semibold">
        Users
      </h1>
      <UButton icon="i-lucide-plus" @click="openCreate">
        New user
      </UButton>
    </div>

    <UTable :data="users ?? []" :columns="columns" :loading="status === 'pending'" />

    <UModal v-model:open="isModalOpen" :title="editingUser ? 'Edit user' : 'New user'">
      <template #body>
        <form class="flex flex-col gap-4" @submit.prevent="submit">
          <UFormField label="Email">
            <UInput v-model="form.email" type="email" :disabled="!!editingUser" class="w-full" />
          </UFormField>

          <UFormField label="Display name">
            <UInput v-model="form.displayName" class="w-full" />
          </UFormField>

          <UFormField v-if="!editingUser" label="Password">
            <UInput v-model="form.password" type="password" class="w-full" />
          </UFormField>

          <UFormField label="Roles">
            <USelectMenu v-model="form.roleNames" :items="roleOptions" multiple class="w-full" />
          </UFormField>

          <UFormField v-if="editingUser" label="Active">
            <USwitch v-model="form.isActive" />
          </UFormField>

          <div class="flex justify-end gap-2">
            <UButton color="neutral" variant="ghost" @click="isModalOpen = false">
              Cancel
            </UButton>
            <UButton type="submit">
              Save
            </UButton>
          </div>
        </form>
      </template>
    </UModal>
  </div>
</template>

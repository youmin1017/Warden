<script setup lang="ts">
import type { PermissionModuleGroup, RoleDetailDto, RoleDto } from '~/types/api'

definePageMeta({ layout: 'admin', permission: 'role.read' })

const { request } = useApi()
const toast = useToast()

const { data: roles, refresh: refreshRoles } = await useAsyncData('admin-roles', () =>
  request<RoleDto[]>('/api/admin/roles')
)
const { data: catalog } = await useAsyncData('admin-permission-catalog', () =>
  request<PermissionModuleGroup[]>('/api/admin/permissions/catalog')
)

const selectedRoleId = ref<string | null>(null)
const selectedRole = ref<RoleDetailDto | null>(null)
const grantedKeys = ref<Set<string>>(new Set())

const isCreateModalOpen = ref(false)
const newRole = reactive({ name: '', description: '' })

async function selectRole(role: RoleDto) {
  selectedRoleId.value = role.id
  selectedRole.value = await request<RoleDetailDto>(`/api/admin/roles/${role.id}`)
  grantedKeys.value = new Set(selectedRole.value.permissions)
}

function isModuleWildcard(module: string) {
  return grantedKeys.value.has('*') || grantedKeys.value.has(`${module}.*`)
}

function togglePermission(key: string, checked: boolean) {
  if (checked) grantedKeys.value.add(key)
  else grantedKeys.value.delete(key)
}

function toggleModuleWildcard(module: string, checked: boolean) {
  const key = `${module}.*`
  if (checked) grantedKeys.value.add(key)
  else grantedKeys.value.delete(key)
}

async function savePermissions() {
  if (!selectedRole.value) return
  try {
    const updated = await request<RoleDetailDto>(`/api/admin/roles/${selectedRole.value.id}/permissions`, {
      method: 'PUT',
      body: { permissions: Array.from(grantedKeys.value) }
    })
    selectedRole.value = updated
    grantedKeys.value = new Set(updated.permissions)
    toast.add({ title: 'Permissions saved', color: 'success' })
  } catch (error: unknown) {
    const message = (error as { data?: { message?: string } })?.data?.message ?? 'Something went wrong.'
    toast.add({ title: 'Error', description: message, color: 'error' })
  }
}

async function createRole() {
  try {
    await request('/api/admin/roles', { method: 'POST', body: { name: newRole.name, description: newRole.description || null } })
    isCreateModalOpen.value = false
    newRole.name = ''
    newRole.description = ''
    await refreshRoles()
    toast.add({ title: 'Role created', color: 'success' })
  } catch (error: unknown) {
    const message = (error as { data?: { message?: string } })?.data?.message ?? 'Something went wrong.'
    toast.add({ title: 'Error', description: message, color: 'error' })
  }
}

async function deleteRole(role: RoleDto) {
  try {
    await request(`/api/admin/roles/${role.id}`, { method: 'DELETE' })
    if (selectedRoleId.value === role.id) {
      selectedRoleId.value = null
      selectedRole.value = null
    }
    await refreshRoles()
  } catch (error: unknown) {
    const message = (error as { data?: { message?: string } })?.data?.message ?? 'Roles that are built-in cannot be deleted.'
    toast.add({ title: 'Error', description: message, color: 'error' })
  }
}
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4">
      <h1 class="text-2xl font-semibold">
        Roles
      </h1>
      <UButton icon="i-lucide-plus" @click="isCreateModalOpen = true">
        New role
      </UButton>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <UCard class="md:col-span-1">
        <div class="flex flex-col gap-1">
          <div
            v-for="role in roles ?? []"
            :key="role.id"
            class="flex items-center justify-between rounded-md px-2 py-1.5 cursor-pointer hover:bg-elevated"
            :class="{ 'bg-elevated': role.id === selectedRoleId }"
            @click="selectRole(role)"
          >
            <div>
              <div class="font-medium">
                {{ role.name }}
              </div>
              <div class="text-xs text-muted">
                {{ role.userCount }} user(s)
              </div>
            </div>
            <UButton
              v-if="!role.isSystemRole"
              size="xs"
              color="error"
              variant="ghost"
              icon="i-lucide-trash-2"
              @click.stop="deleteRole(role)"
            />
          </div>
        </div>
      </UCard>

      <UCard class="md:col-span-2">
        <template v-if="selectedRole">
          <div class="flex items-center justify-between mb-4">
            <div>
              <h2 class="text-lg font-semibold">
                {{ selectedRole.name }}
              </h2>
              <p class="text-sm text-muted">
                {{ selectedRole.description }}
              </p>
            </div>
            <UButton :disabled="selectedRole.isSystemRole" @click="savePermissions">
              Save permissions
            </UButton>
          </div>

          <UAlert
            v-if="selectedRole.isSystemRole"
            color="neutral"
            variant="subtle"
            title="This is a system role and always has every permission."
            class="mb-4"
          />

          <div v-else class="flex flex-col gap-4">
            <div v-for="group in catalog ?? []" :key="group.module">
              <div class="flex items-center gap-2 mb-2">
                <UCheckbox
                  :model-value="isModuleWildcard(group.module)"
                  :label="`All ${group.module} permissions`"
                  @update:model-value="(v) => toggleModuleWildcard(group.module, !!v)"
                />
              </div>
              <div class="grid grid-cols-2 gap-2 pl-6">
                <UCheckbox
                  v-for="permission in group.permissions"
                  :key="permission.key"
                  :model-value="isModuleWildcard(group.module) || grantedKeys.has(permission.key)"
                  :disabled="isModuleWildcard(group.module)"
                  :label="permission.key"
                  @update:model-value="(v) => togglePermission(permission.key, !!v)"
                />
              </div>
            </div>
          </div>
        </template>
        <template v-else>
          <p class="text-muted">
            Select a role to manage its permissions.
          </p>
        </template>
      </UCard>
    </div>

    <UModal v-model:open="isCreateModalOpen" title="New role">
      <template #body>
        <form class="flex flex-col gap-4" @submit.prevent="createRole">
          <UFormField label="Name">
            <UInput v-model="newRole.name" class="w-full" />
          </UFormField>
          <UFormField label="Description">
            <UInput v-model="newRole.description" class="w-full" />
          </UFormField>
          <div class="flex justify-end gap-2">
            <UButton color="neutral" variant="ghost" @click="isCreateModalOpen = false">
              Cancel
            </UButton>
            <UButton type="submit">
              Create
            </UButton>
          </div>
        </form>
      </template>
    </UModal>
  </div>
</template>

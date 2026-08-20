<script setup lang="ts">
import type { MenuItemDto, PermissionModuleGroup } from '~/types/api'

definePageMeta({ layout: 'admin' })

const { request } = useApi()
const toast = useToast()

const { data: tree, refresh: refreshTree } = await useAsyncData('admin-menu-tree', () =>
  request<MenuItemDto[]>('/api/admin/menu')
)
const { data: catalog } = await useAsyncData('admin-menu-permission-catalog', () =>
  request<PermissionModuleGroup[]>('/api/admin/permissions/catalog')
)

const permissionOptions = computed(() => (catalog.value ?? []).flatMap((g) => g.permissions.map((p) => p.key)))

interface FlatRow {
  item: MenuItemDto
  depth: number
}

function flatten(nodes: MenuItemDto[], depth = 0): FlatRow[] {
  return nodes.flatMap((node) => [{ item: node, depth }, ...flatten(node.children, depth + 1)])
}

const rows = computed(() => flatten(tree.value ?? []))
const parentOptions = computed(() => [
  { label: '(top level)', value: null },
  ...rows.value.map((r) => ({ label: `${'— '.repeat(r.depth)}${r.item.label}`, value: r.item.id }))
])

const isModalOpen = ref(false)
const editingItem = ref<MenuItemDto | null>(null)

const form = reactive({
  parentId: null as string | null,
  label: '',
  path: '',
  icon: '',
  requiredPermission: undefined as string | undefined,
  sortOrder: 0,
  isActive: true
})

function openCreate() {
  editingItem.value = null
  Object.assign(form, {
    parentId: null,
    label: '',
    path: '',
    icon: '',
    requiredPermission: undefined,
    sortOrder: 0,
    isActive: true
  })
  isModalOpen.value = true
}

function openEdit(item: MenuItemDto) {
  editingItem.value = item
  Object.assign(form, {
    parentId: item.parentId,
    label: item.label,
    path: item.path ?? '',
    icon: item.icon ?? '',
    requiredPermission: item.requiredPermission ?? undefined,
    sortOrder: item.sortOrder,
    isActive: item.isActive
  })
  isModalOpen.value = true
}

async function submit() {
  const body = {
    parentId: form.parentId,
    label: form.label,
    path: form.path || null,
    icon: form.icon || null,
    requiredPermission: form.requiredPermission ?? null,
    sortOrder: form.sortOrder,
    isActive: form.isActive
  }

  try {
    if (editingItem.value) {
      await request(`/api/admin/menu/${editingItem.value.id}`, { method: 'PUT', body })
    } else {
      await request('/api/admin/menu', { method: 'POST', body })
    }
    isModalOpen.value = false
    await refreshTree()
    toast.add({ title: 'Saved', color: 'success' })
  } catch (error: unknown) {
    const message = (error as { data?: { message?: string } })?.data?.message ?? 'Something went wrong.'
    toast.add({ title: 'Error', description: message, color: 'error' })
  }
}

async function remove(item: MenuItemDto) {
  await request(`/api/admin/menu/${item.id}`, { method: 'DELETE' })
  await refreshTree()
  toast.add({ title: 'Menu item deleted', color: 'success' })
}
</script>

<template>
  <div>
    <div class="mb-4 flex items-center justify-between">
      <h1 class="text-2xl font-semibold">Menu</h1>
      <UButton icon="i-lucide-plus" @click="openCreate">New item</UButton>
    </div>

    <UCard>
      <div class="flex flex-col divide-y divide-default">
        <div v-for="row in rows" :key="row.item.id" class="flex items-center justify-between py-2">
          <div class="flex items-center gap-2" :style="{ paddingLeft: `${row.depth * 1.5}rem` }">
            <UIcon v-if="row.item.icon" :name="row.item.icon" class="size-4" />
            <span class="font-medium">{{ row.item.label }}</span>
            <span v-if="row.item.path" class="text-xs text-muted">{{ row.item.path }}</span>
            <UBadge v-if="row.item.requiredPermission" size="sm" color="neutral" variant="subtle">
              {{ row.item.requiredPermission }}
            </UBadge>
            <UBadge v-if="!row.item.isActive" size="sm" color="warning" variant="subtle">Inactive</UBadge>
          </div>
          <div class="flex gap-2">
            <UButton size="xs" color="neutral" variant="ghost" icon="i-lucide-pencil" @click="openEdit(row.item)" />
            <UButton size="xs" color="error" variant="ghost" icon="i-lucide-trash-2" @click="remove(row.item)" />
          </div>
        </div>
        <p v-if="!rows.length" class="py-4 text-muted">No menu items yet.</p>
      </div>
    </UCard>

    <UModal v-model:open="isModalOpen" :title="editingItem ? 'Edit menu item' : 'New menu item'">
      <template #body>
        <form class="flex flex-col gap-4" @submit.prevent="submit">
          <UFormField label="Parent">
            <USelectMenu
              v-model="form.parentId"
              :items="parentOptions"
              value-key="value"
              label-key="label"
              class="w-full"
            />
          </UFormField>

          <UFormField label="Label">
            <UInput v-model="form.label" class="w-full" />
          </UFormField>

          <UFormField label="Path">
            <UInput v-model="form.path" placeholder="/admin/example" class="w-full" />
          </UFormField>

          <UFormField label="Icon">
            <UInput v-model="form.icon" placeholder="i-lucide-star" class="w-full" />
          </UFormField>

          <UFormField label="Required permission">
            <USelectMenu
              v-model="form.requiredPermission"
              :items="permissionOptions"
              class="w-full"
              placeholder="(none — visible to any signed-in user)"
            />
          </UFormField>

          <UFormField label="Sort order">
            <UInputNumber v-model="form.sortOrder" class="w-full" />
          </UFormField>

          <UFormField label="Active">
            <USwitch v-model="form.isActive" />
          </UFormField>

          <div class="flex justify-end gap-2">
            <UButton color="neutral" variant="ghost" @click="isModalOpen = false">Cancel</UButton>
            <UButton type="submit">Save</UButton>
          </div>
        </form>
      </template>
    </UModal>
  </div>
</template>

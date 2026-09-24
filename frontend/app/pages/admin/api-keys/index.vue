<script setup lang="ts">
import { h, resolveComponent } from 'vue'
import type { TableColumn } from '@nuxt/ui'
import type { ApiKeyCreatedDto, ApiKeyDto, PermissionModuleGroup } from '~/types/api'

definePageMeta({ layout: 'admin', permission: 'apikey.read' })

const { request } = useApi()
const toast = useToast()

const { data: apiKeys, refresh: refreshApiKeys, status } = await useAsyncData('admin-api-keys', () =>
  request<ApiKeyDto[]>('/api/admin/api-keys')
)
const { data: catalog } = await useAsyncData('admin-permission-catalog', () =>
  request<PermissionModuleGroup[]>('/api/admin/permissions/catalog')
)

const EXPIRY_OPTIONS: string[] = ['Never', '30 days', '90 days', '1 year']
const EXPIRY_DAYS: Record<string, number | null> = { 'Never': null, '30 days': 30, '90 days': 90, '1 year': 365 }

const isCreateModalOpen = ref(false)
const newKey = reactive({ name: '', expiresIn: 'Never' as string })
const selectedScopes = ref<Set<string>>(new Set())
const createdKey = ref<ApiKeyCreatedDto | null>(null)

function isModuleWildcard(module: string) {
  return selectedScopes.value.has('*') || selectedScopes.value.has(`${module}.*`)
}

function toggleScope(key: string, checked: boolean) {
  if (checked) selectedScopes.value.add(key)
  else selectedScopes.value.delete(key)
}

function toggleModuleWildcard(module: string, checked: boolean) {
  const key = `${module}.*`
  if (checked) selectedScopes.value.add(key)
  else selectedScopes.value.delete(key)
}

function openCreate() {
  newKey.name = ''
  newKey.expiresIn = 'Never'
  selectedScopes.value = new Set()
  isCreateModalOpen.value = true
}

async function createApiKey() {
  const days = EXPIRY_DAYS[newKey.expiresIn] ?? null
  const expiresAtUtc = days ? new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString() : null

  try {
    const created = await request<ApiKeyCreatedDto>('/api/admin/api-keys', {
      method: 'POST',
      body: { name: newKey.name, expiresAtUtc, scopes: Array.from(selectedScopes.value) }
    })
    isCreateModalOpen.value = false
    createdKey.value = created
    await refreshApiKeys()
  } catch (error: unknown) {
    const message = (error as { data?: { message?: string } })?.data?.message ?? 'Something went wrong.'
    toast.add({ title: 'Error', description: message, color: 'error' })
  }
}

async function revokeApiKey(key: ApiKeyDto) {
  try {
    await request(`/api/admin/api-keys/${key.id}/revoke`, { method: 'POST' })
    await refreshApiKeys()
    toast.add({ title: 'API key revoked', color: 'success' })
  } catch (error: unknown) {
    const message = (error as { data?: { message?: string } })?.data?.message ?? 'Something went wrong.'
    toast.add({ title: 'Error', description: message, color: 'error' })
  }
}

async function deleteApiKey(key: ApiKeyDto) {
  try {
    await request(`/api/admin/api-keys/${key.id}`, { method: 'DELETE' })
    await refreshApiKeys()
    toast.add({ title: 'API key deleted', color: 'success' })
  } catch (error: unknown) {
    const message = (error as { data?: { message?: string } })?.data?.message ?? 'Something went wrong.'
    toast.add({ title: 'Error', description: message, color: 'error' })
  }
}

const pendingAction = ref<{ kind: 'revoke' | 'delete', key: ApiKeyDto } | null>(null)
const isConfirming = ref(false)

async function confirmPendingAction() {
  if (!pendingAction.value) return
  const { kind, key } = pendingAction.value
  isConfirming.value = true
  try {
    await (kind === 'revoke' ? revokeApiKey(key) : deleteApiKey(key))
  } finally {
    isConfirming.value = false
    pendingAction.value = null
  }
}

async function copyRawKey() {
  if (!createdKey.value) return
  await navigator.clipboard.writeText(createdKey.value.rawKey)
  toast.add({ title: 'Copied to clipboard', color: 'success' })
}

function keyStatus(key: ApiKeyDto): { label: string, color: 'success' | 'error' | 'neutral' } {
  if (key.revokedAtUtc) return { label: 'Revoked', color: 'neutral' }
  if (key.expiresAtUtc && new Date(key.expiresAtUtc) <= new Date()) return { label: 'Expired', color: 'error' }
  return { label: 'Active', color: 'success' }
}

const UButton = resolveComponent('UButton')
const UBadge = resolveComponent('UBadge')

const columns: TableColumn<ApiKeyDto>[] = [
  { accessorKey: 'name', header: 'Name' },
  {
    id: 'key',
    header: 'Key',
    cell: ({ row }) => h('code', { class: 'text-xs text-muted' }, `wdn_${row.original.keyId}••••${row.original.displaySuffix}`)
  },
  {
    accessorKey: 'scopes',
    header: 'Scopes',
    cell: ({ row }) => h('div', { class: 'flex gap-1 flex-wrap' }, row.original.scopes.map(s => h(UBadge, { key: s, color: 'neutral', variant: 'subtle' }, () => s)))
  },
  {
    id: 'status',
    header: 'Status',
    cell: ({ row }) => {
      const s = keyStatus(row.original)
      return h(UBadge, { color: s.color, variant: 'subtle' }, () => s.label)
    }
  },
  {
    accessorKey: 'lastUsedAtUtc',
    header: 'Last used',
    cell: ({ row }) => row.original.lastUsedAtUtc ? new Date(row.original.lastUsedAtUtc).toLocaleString() : 'Never'
  },
  {
    id: 'actions',
    header: '',
    cell: ({ row }) => h('div', { class: 'flex justify-end gap-1' }, [
      row.original.revokedAtUtc
        ? null
        : h(UButton, { size: 'xs', color: 'warning', variant: 'ghost', icon: 'i-lucide-ban', onClick: () => (pendingAction.value = { kind: 'revoke', key: row.original }) }, () => 'Revoke'),
      h(UButton, { size: 'xs', color: 'error', variant: 'ghost', icon: 'i-lucide-trash-2', onClick: () => (pendingAction.value = { kind: 'delete', key: row.original }) }, () => 'Delete')
    ])
  }
]
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-4">
      <h1 class="text-2xl font-semibold">
        API Keys
      </h1>
      <UButton icon="i-lucide-plus" @click="openCreate">
        New API key
      </UButton>
    </div>

    <UTable :data="apiKeys ?? []" :columns="columns" :loading="status === 'pending'" />

    <UModal v-model:open="isCreateModalOpen" title="New API key">
      <template #body>
        <form class="flex flex-col gap-4" @submit.prevent="createApiKey">
          <UFormField label="Name">
            <UInput v-model="newKey.name" placeholder="e.g. CI pipeline" class="w-full" />
          </UFormField>

          <UFormField label="Expires">
            <USelectMenu v-model="newKey.expiresIn" :items="EXPIRY_OPTIONS" class="w-full" />
          </UFormField>

          <UFormField label="Scopes">
            <p class="text-sm text-muted mb-2">
              The key can only be granted permissions you already have.
            </p>
            <div class="flex flex-col gap-4">
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
                    :model-value="isModuleWildcard(group.module) || selectedScopes.has(permission.key)"
                    :disabled="isModuleWildcard(group.module)"
                    :label="permission.key"
                    @update:model-value="(v) => toggleScope(permission.key, !!v)"
                  />
                </div>
              </div>
            </div>
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

    <UModal
      :open="!!pendingAction"
      :title="pendingAction?.kind === 'delete' ? 'Delete API key' : 'Revoke API key'"
      @update:open="(open) => { if (!open && !isConfirming) pendingAction = null }"
    >
      <template #body>
        <div v-if="pendingAction" class="flex flex-col gap-4">
          <p v-if="pendingAction.kind === 'revoke'">
            Revoke <strong>{{ pendingAction.key.name }}</strong>? Anything using this key will immediately
            get 401 responses. The key stays listed as Revoked and can't be re-enabled.
          </p>
          <p v-else>
            Permanently delete <strong>{{ pendingAction.key.name }}</strong>?
            <template v-if="!pendingAction.key.revokedAtUtc">
              It's still active — anything using it will immediately get 401 responses.
            </template>
            This can't be undone, and the key will no longer appear in this list.
          </p>
          <code class="text-xs text-muted">wdn_{{ pendingAction.key.keyId }}••••{{ pendingAction.key.displaySuffix }}</code>
          <div class="flex justify-end gap-2">
            <UButton color="neutral" variant="ghost" :disabled="isConfirming" @click="pendingAction = null">
              Cancel
            </UButton>
            <UButton
              :color="pendingAction.kind === 'delete' ? 'error' : 'warning'"
              :loading="isConfirming"
              @click="confirmPendingAction"
            >
              {{ pendingAction.kind === 'delete' ? 'Delete' : 'Revoke' }}
            </UButton>
          </div>
        </div>
      </template>
    </UModal>

    <UModal :open="!!createdKey" title="API key created" :close="false" :dismissible="false">
      <template #body>
        <div v-if="createdKey" class="flex flex-col gap-4">
          <UAlert
            color="warning"
            variant="subtle"
            title="Copy this key now — it won't be shown again."
          />
          <div class="flex items-center gap-2">
            <code class="flex-1 rounded-md bg-elevated px-3 py-2 text-sm break-all">{{ createdKey.rawKey }}</code>
            <UButton icon="i-lucide-copy" color="neutral" variant="soft" @click="copyRawKey">
              Copy
            </UButton>
          </div>
          <div class="flex justify-end">
            <UButton @click="createdKey = null">
              Done
            </UButton>
          </div>
        </div>
      </template>
    </UModal>
  </div>
</template>

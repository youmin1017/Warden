import { defineStore } from 'pinia'
import type { CurrentUserDto, TokenPairDto } from '~/types/api'

export const useAuthStore = defineStore('auth', () => {
  // Captured once, synchronously, at store setup — composables like useRuntimeConfig()
  // can silently lose Nuxt's request context if called from inside an action body after
  // an `await` (this bit us during SSR reloads: the post-await call was a no-op there).
  const apiBase = useRuntimeConfig().public.apiBase

  const accessToken = ref<string | null>(null)
  const user = ref<CurrentUserDto | null>(null)
  const refreshToken = useCookie<string | null>('warden_refresh_token', {
    sameSite: 'strict',
    maxAge: 60 * 60 * 24 * 14
  })

  const isAuthenticated = computed(() => !!accessToken.value && !!user.value)

  // Guards against a second, re-entrant refresh() call (e.g. Nuxt re-running global
  // middleware for a 404 fallback render within the same SSR request) racing the first —
  // refresh tokens are single-use/rotating, so a second call with the same token would be
  // rejected by the backend and wrongly clear a session that was just successfully restored.
  let inFlightRefresh: Promise<boolean> | null = null

  function setTokens(pair: TokenPairDto) {
    accessToken.value = pair.accessToken
    refreshToken.value = pair.refreshToken
  }

  function clearSession() {
    accessToken.value = null
    user.value = null
    refreshToken.value = null
  }

  /** Wildcard-aware check mirroring the backend's PermissionMatcher. */
  function hasPermission(key: string): boolean {
    const granted = user.value?.permissions ?? []
    return granted.some((g) => {
      if (g === '*' || g === key) return true
      if (g.endsWith('.*')) return key.startsWith(g.slice(0, -1))
      return false
    })
  }

  async function fetchCurrentUser() {
    user.value = await $fetch<CurrentUserDto>('/api/auth/me', {
      baseURL: apiBase,
      headers: { Authorization: `Bearer ${accessToken.value}` }
    })
  }

  async function login(email: string, password: string) {
    const pair = await $fetch<TokenPairDto>('/api/auth/login', {
      baseURL: apiBase,
      method: 'POST',
      body: { email, password }
    })
    setTokens(pair)
    await fetchCurrentUser()
  }

  async function refresh(): Promise<boolean> {
    if (!refreshToken.value) return false
    if (inFlightRefresh) return inFlightRefresh

    inFlightRefresh = (async () => {
      try {
        const pair = await $fetch<TokenPairDto>('/api/auth/refresh', {
          baseURL: apiBase,
          method: 'POST',
          body: { refreshToken: refreshToken.value }
        })
        setTokens(pair)
        await fetchCurrentUser()
        return true
      } catch (error: unknown) {
        // Only a real rejection from the backend (refresh token invalid/expired/revoked) should
        // log the user out. A transient failure (backend restarting, network blip) must NOT wipe
        // the refresh token cookie — that would force a full re-login for no good reason.
        const status = (error as { response?: { status?: number } })?.response?.status
        if (status === 401 || status === 400) {
          clearSession()
        }
        return false
      } finally {
        inFlightRefresh = null
      }
    })()

    return inFlightRefresh
  }

  async function restoreSession(): Promise<boolean> {
    if (isAuthenticated.value) return true
    return await refresh()
  }

  async function logout() {
    if (refreshToken.value) {
      try {
        await $fetch('/api/auth/logout', {
          baseURL: apiBase,
          method: 'POST',
          body: { refreshToken: refreshToken.value }
        })
      } catch {
        // best-effort — clear local session regardless
      }
    }
    clearSession()
  }

  return {
    accessToken,
    user,
    isAuthenticated,
    hasPermission,
    login,
    logout,
    refresh,
    restoreSession
  }
})

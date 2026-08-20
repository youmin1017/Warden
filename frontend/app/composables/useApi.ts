import { useAuthStore } from '~/stores/auth'

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH'
  body?: Record<string, unknown> | unknown[]
  __isRetry?: boolean
}

/** Attaches the bearer token to every call and retries once after a silent refresh on 401. */
export function useApi() {
  const auth = useAuthStore()
  const config = useRuntimeConfig()

  async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
    try {
      return await $fetch<T>(path, {
        baseURL: config.public.apiBase,
        method: opts.method ?? 'GET',
        body: opts.body,
        headers: auth.accessToken ? { Authorization: `Bearer ${auth.accessToken}` } : undefined
      })
    } catch (error: unknown) {
      const status = (error as { response?: { status?: number } })?.response?.status
      if (status === 401 && !opts.__isRetry) {
        const refreshed = await auth.refresh()
        if (refreshed) {
          return request<T>(path, { ...opts, __isRetry: true })
        }
        await navigateTo('/login')
      }
      throw error
    }
  }

  return { request }
}

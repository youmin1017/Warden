import { useAuthStore } from '~/stores/auth'

// Does the code exchange here, not in the page component's <script setup> — a raw top-level
// await there re-runs on the client during hydration (Nuxt only dedupes useAsyncData/useFetch
// across the SSR->CSR boundary), and since the handoff code is single-use, the second run would
// fail against a mismatched auth state and produce a hydration mismatch between the SSR-rendered
// callback card and the client's post-login view. Middleware doesn't have that problem: `return
// navigateTo(...)` here becomes a real HTTP redirect during SSR, so the page component never
// actually renders in either the success or failure case.
export default defineNuxtRouteMiddleware(async (to) => {
  const code = to.query.code
  if (typeof code !== 'string' || !code) {
    return navigateTo('/login?error=oidc_failed')
  }

  const auth = useAuthStore()
  try {
    await auth.completeOidcLogin(code)
  } catch {
    return navigateTo('/login?error=oidc_failed')
  }

  return navigateTo('/admin')
})

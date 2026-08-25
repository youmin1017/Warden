import { useAuthStore } from '~/stores/auth'

export default defineNuxtRouteMiddleware(async (to) => {
  const auth = useAuthStore()

  if (!auth.isAuthenticated) {
    await auth.restoreSession()
  }

  // /auth/callback must be reachable while unauthenticated — it's where the OIDC handoff
  // code gets exchanged for a session in the first place.
  const isPublicAuthPage = to.path === '/login' || to.path === '/auth/callback'

  if (to.path === '/') {
    return navigateTo(auth.isAuthenticated ? '/admin' : '/login')
  }

  if (!auth.isAuthenticated && !isPublicAuthPage) {
    return navigateTo('/login')
  }

  if (auth.isAuthenticated && isPublicAuthPage) {
    return navigateTo('/admin')
  }

  const requiredPermission = to.meta.permission
  if (requiredPermission && !auth.hasPermission(requiredPermission)) {
    throw createError({ statusCode: 403, statusMessage: 'Forbidden' })
  }
})

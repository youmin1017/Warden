import { useAuthStore } from '~/stores/auth'

export default defineNuxtRouteMiddleware(async (to) => {
  const auth = useAuthStore()

  if (!auth.isAuthenticated) {
    await auth.restoreSession()
  }

  const isLoginPage = to.path === '/login'

  if (to.path === '/') {
    return navigateTo(auth.isAuthenticated ? '/admin' : '/login')
  }

  if (!auth.isAuthenticated && !isLoginPage) {
    return navigateTo('/login')
  }

  if (auth.isAuthenticated && isLoginPage) {
    return navigateTo('/admin')
  }
})

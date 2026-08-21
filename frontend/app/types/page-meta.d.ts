declare module '#app' {
  interface PageMeta {
    permission?: string
  }
}

declare module 'vue-router' {
  interface RouteMeta {
    permission?: string
  }
}

export {}

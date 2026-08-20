export interface UserDto {
  id: string
  email: string
  displayName: string
  isActive: boolean
  roles: string[]
  createdAtUtc: string
}

export interface RoleDto {
  id: string
  name: string
  description: string | null
  isSystemRole: boolean
  userCount: number
}

export interface RoleDetailDto {
  id: string
  name: string
  description: string | null
  isSystemRole: boolean
  permissions: string[]
}

export interface MenuItemDto {
  id: string
  parentId: string | null
  label: string
  path: string | null
  icon: string | null
  requiredPermission: string | null
  sortOrder: number
  isActive: boolean
  children: MenuItemDto[]
}

export interface PermissionDescriptor {
  key: string
  module: string
  controller: string
  action: string
  httpMethod: string | null
}

export interface PermissionModuleGroup {
  module: string
  permissions: PermissionDescriptor[]
}

export interface CurrentUserDto {
  id: string
  email: string
  displayName: string
  roles: string[]
  permissions: string[]
}

export interface TokenPairDto {
  accessToken: string
  accessTokenExpiresAtUtc: string
  refreshToken: string
  refreshTokenExpiresAtUtc: string
}

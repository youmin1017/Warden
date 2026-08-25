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

export interface OidcProviderDto {
  name: string
  displayName: string
}

export interface ApiKeyDto {
  id: string
  name: string
  keyId: string
  displaySuffix: string
  createdAtUtc: string
  expiresAtUtc: string | null
  revokedAtUtc: string | null
  lastUsedAtUtc: string | null
  scopes: string[]
}

export interface ApiKeyCreatedDto {
  apiKey: ApiKeyDto
  rawKey: string
}

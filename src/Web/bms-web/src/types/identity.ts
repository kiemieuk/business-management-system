export interface UserProfile {
  id: string
  username: string
  email: string
  fullName: string
  roles: string[]
  permissions: string[]
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  expiresAt: string
  user: UserProfile
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface RoleSummary {
  id: string
  name: string
}

export interface UserDto {
  id: string
  username: string
  email: string
  fullName: string
  isActive: boolean
  roles: RoleSummary[]
  createdAt: string
  lastLoginAt: string | null
}

export interface CreateUserRequest {
  username: string
  email: string
  fullName: string
  password: string
  roleIds: string[]
}

export interface UpdateUserRequest {
  email: string
  fullName: string
  isActive: boolean
}

export interface RoleDto {
  id: string
  name: string
  description: string | null
  isSystem: boolean
  permissions: string[]
  userCount: number
}

export interface CreateRoleRequest {
  name: string
  description: string | null
  permissions: string[]
}

export interface UpdateRoleRequest {
  name: string
  description: string | null
}

export interface PermissionDto {
  code: string
  name: string
  group: string
  description: string | null
}

export const Permissions = {
  UsersView: 'identity.users.view',
  UsersCreate: 'identity.users.create',
  UsersUpdate: 'identity.users.update',
  UsersDelete: 'identity.users.delete',
  RolesView: 'identity.roles.view',
  RolesCreate: 'identity.roles.create',
  RolesUpdate: 'identity.roles.update',
  RolesDelete: 'identity.roles.delete',
} as const

import { api } from './client'
import type {
  ChangePasswordRequest,
  CreateRoleRequest,
  CreateUserRequest,
  LoginRequest,
  LoginResponse,
  PermissionDto,
  RoleDto,
  UpdateRoleRequest,
  UpdateUserRequest,
  UserDto,
  UserProfile,
} from '../types/identity'

const base = '/identity'
const id = (value: string) => encodeURIComponent(value)

export const authApi = {
  login: (request: LoginRequest) =>
    api.post<LoginResponse>(`${base}/auth/login`, request, { anonymous: true }),
  me: () => api.get<UserProfile>(`${base}/auth/me`),
  changePassword: (request: ChangePasswordRequest) =>
    api.post<void>(`${base}/auth/change-password`, request),
}

export const usersApi = {
  list: () => api.get<UserDto[]>(`${base}/users`),
  get: (userId: string) => api.get<UserDto>(`${base}/users/${id(userId)}`),
  create: (request: CreateUserRequest) => api.post<UserDto>(`${base}/users`, request),
  update: (userId: string, request: UpdateUserRequest) =>
    api.put<UserDto>(`${base}/users/${id(userId)}`, request),
  setRoles: (userId: string, roleIds: string[]) =>
    api.put<UserDto>(`${base}/users/${id(userId)}/roles`, { roleIds }),
  resetPassword: (userId: string, newPassword: string) =>
    api.post<void>(`${base}/users/${id(userId)}/reset-password`, { newPassword }),
  delete: (userId: string) => api.delete(`${base}/users/${id(userId)}`),
}

export const rolesApi = {
  list: () => api.get<RoleDto[]>(`${base}/roles`),
  get: (roleId: string) => api.get<RoleDto>(`${base}/roles/${id(roleId)}`),
  create: (request: CreateRoleRequest) => api.post<RoleDto>(`${base}/roles`, request),
  update: (roleId: string, request: UpdateRoleRequest) =>
    api.put<RoleDto>(`${base}/roles/${id(roleId)}`, request),
  setPermissions: (roleId: string, permissions: string[]) =>
    api.put<RoleDto>(`${base}/roles/${id(roleId)}/permissions`, { permissions }),
  delete: (roleId: string) => api.delete(`${base}/roles/${id(roleId)}`),
}

export const permissionsApi = {
  list: () => api.get<PermissionDto[]>(`${base}/permissions`),
}

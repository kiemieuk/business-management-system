import { Permissions } from '../types/identity'

export interface NavItem {
  label: string
  path: string
  /** Permission code required to see this item. */
  permission?: string
}

export interface NavSection {
  title?: string
  items: NavItem[]
}

/**
 * Sidebar navigation. Future business modules (Inventory, Sales, HR, ...)
 * register their entries here as new sections.
 */
export const navigation: NavSection[] = [
  {
    items: [{ label: 'Dashboard', path: '/' }],
  },
  {
    title: 'Administration',
    items: [
      { label: 'Users', path: '/users', permission: Permissions.UsersView },
      { label: 'Roles', path: '/roles', permission: Permissions.RolesView },
    ],
  },
]

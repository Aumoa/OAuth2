export const oauth2ActionPaths = {
  manageAccount: '/actions/manage-account',
  logout: '/actions/logout',
} as const;

export type OAuth2ActionPath = typeof oauth2ActionPaths[keyof typeof oauth2ActionPaths];

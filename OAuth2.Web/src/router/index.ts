import { createRouter, createWebHistory, type RouteLocationNormalized } from 'vue-router';
import { oauth2ActionPaths } from '../shared/oauth2/src/action-paths.ts';

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: oauth2ActionPaths.manageAccount,
      redirect: '/',
    },
    {
      path: oauth2ActionPaths.logout,
      component: () => import('../layouts/UnauthorizedLayout.vue'),
      meta: {
        authentication: 'optional',
      },
      children: [
        {
          path: '',
          component: () => import('../views/actions/LogoutActionView.vue'),
        },
      ],
    },
    {
      path: '/',
      component: () => import('../layouts/AuthorizedLayout.vue'),
      meta: {
        authentication: 'required',
      },
      children: [
        {
          path: '',
          component: () => import('../views/HomeView.vue'),
        },
        {
          path: 'applications',
          children: [
            {
              path: '',
              name: 'applications',
              component: () => import('../views/ApplicationGroupsView.vue'),
            },
            {
              path: 'personal',
              name: 'applications-personal',
              component: () => import('../views/ManageApplicationsView.vue'),
            },
            {
              path: 'personal/new',
              name: 'applications-personal-new',
              component: () => import('../views/CreateApplicationView.vue'),
            },
            {
              path: 'personal/edit/:clientId(.+)',
              name: 'applications-personal-edit',
              component: () => import('../views/EditApplicationView.vue'),
            },
            {
              path: 'organizations/:organizationId',
              name: 'applications-organization',
              component: () => import('../views/ManageApplicationsView.vue'),
            },
            {
              path: 'organizations/:organizationId/new',
              name: 'applications-organization-new',
              component: () => import('../views/CreateApplicationView.vue'),
            },
            {
              path: 'organizations/:organizationId/edit/:clientId(.+)',
              name: 'applications-organization-edit',
              component: () => import('../views/EditApplicationView.vue'),
            },
            {
              path: 'new',
              redirect: { name: 'applications-personal-new' },
            },
            {
              path: 'edit/:clientId(.+)',
              redirect: route => ({
                name: 'applications-personal-edit',
                params: { clientId: route.params.clientId },
              }),
            },
          ],
        },
      ],
    },
    {
      path: '/',
      component: () => import('../layouts/UnauthorizedLayout.vue'),
      meta: {
        authentication: 'optional',
      },
      children: [
        {
          path: 'checking',
          component: () => import('../views/CheckingView.vue'),
        },
        {
          path: 'login',
          component: () => import('../views/Login.vue'),
        },
        {
          path: 'register',
          component: () => import('../views/Register.vue'),
        },
        {
          path: 'error',
          component: () => import('../views/Error.vue'),
        },
        {
          path: 'verifyEmail',
          component: () => import('../views/VerifyEmail.vue'),
        },
      ],
    },
  ],
});

export function requiresAuthentication(route: RouteLocationNormalized): boolean {
  return route.meta.authentication === 'required';
}

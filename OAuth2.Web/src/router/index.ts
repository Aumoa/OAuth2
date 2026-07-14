import { createRouter, createWebHistory } from 'vue-router';

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: () => import('../layouts/AuthorizedLayout.vue'),
      children: [
        {
          path: '',
          component: () => import('../views/HomeView.vue')
        }
      ]
    },
    {
      path: '/',
      component: () => import('../layouts/UnauthorizedLayout.vue'),
      children: [
        {
          path: 'checking',
          component: () => import('../views/CheckingView.vue')
        },
        {
          path: 'login',
          component: () => import('../views/Login.vue')
        },
        {
          path: 'register',
          component: () => import('../views/Register.vue')
        },
        {
          path: 'error',
          component: () => import('../views/Error.vue')
        },
        {
          path: 'verifyEmail',
          component: () => import('../views/VerifyEmail.vue')
        }
      ]
    }
  ],
});

const publicRoutePaths = [
  '/login',
  '/register',
  '/verifyEmail',
  '/error',
];

export function requiredAuthenticated(route?: string): boolean {
  route = route ?? router.currentRoute.value.path;
  return !publicRoutePaths.some(s => route.startsWith(s));
}

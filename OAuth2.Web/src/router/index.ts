import { createRouter, createWebHistory } from 'vue-router'

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../views/HomeView.vue'),
    },
    {
      path: '/checking',
      name: 'checking',
      component: () => import('../views/CheckingView.vue')
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/Authorize.vue')
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('../views/Authorize.vue')
    },
    {
      path: '/error',
      name: 'error',
      component: () => import('../views/Error.vue')
    }
  ],
})

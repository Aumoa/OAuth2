import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { RouteError } from './router/route-error';

export type AuthStatus = 'checking' | 'authenticated' | 'unauthenticated' | 'error';

export interface User {
  id: string;
  sub: string;
  email: string;
  profile: string;
};

export const useAuthStore = defineStore('auth', () => {
  const status = ref<
    'checking' | 'authenticated' | 'unauthenticated' | 'error'
  >('checking');

  const user = ref<User | null>(null);

  const isAuthenticated = computed(
    () => status.value === 'authenticated',
  );

  async function initializeAsync() {
    status.value = 'checking'

    try {
      console.log('Fetching session...');      
      const response = await fetch('/api/v1/session', {
        credentials: 'include',
      });

      if (response.status === 401) {
        user.value = null;
        status.value = 'unauthenticated';
        console.log('Session request returned 401 Unauthorized. User is unauthenticated.');
        throw new RouteError('/login');
      }

      if (!response.ok) {
        throw new Error(`Session request failed: ${response.status}`);
      }

      user.value = await response.json();
      status.value = 'authenticated';
    } catch (error) {
      if (error instanceof RouteError) {
        throw error;
      }

      console.error(`Error occurred while fetching session: ${error}`);
      user.value = null;
      status.value = 'error';
      throw error;
    }
  }

  async function logoutAsync() {
    await fetch('/api/v1/logout', {
      method: 'POST',
      credentials: 'include',
    });

    user.value = null;
    status.value = 'unauthenticated';
  }

  return {
    status,
    user,
    isAuthenticated,
    initializeAsync,
    logoutAsync,
  };
});
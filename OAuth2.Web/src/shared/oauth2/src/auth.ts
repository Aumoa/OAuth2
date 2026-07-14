import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

export type AuthStatus =
  | 'checking'
  | 'authenticated'
  | 'unauthenticated'
  | 'error';

export interface User {
  sub: string;

  id?: string;
  picture?: string;
  email?: string;
  name?: string;
}

export const useAuthStore = defineStore('auth', () => {
  const status = ref<AuthStatus>('checking');
  const user = ref<User | null>(null);

  let initializationPromise: Promise<void> | null = null;

  const isAuthenticated = computed(
    () => status.value === 'authenticated',
  );

  function setUnauthenticated(): void {
    user.value = null;
    status.value = 'unauthenticated';
  }

  async function loadSessionAsync(): Promise<void> {
    try {
      const response = await fetch('/api/v1/session', {
        credentials: 'include',
        headers: {
          Accept: 'application/json',
        },
      });

      if (response.status === 401) {
        setUnauthenticated();
        return;
      }

      if (!response.ok) {
        setUnauthenticated();
        return;
      }

      user.value = await response.json() as User;
      status.value = 'authenticated';
    } catch (error) {
      user.value = null;
      status.value = 'error';
      throw error;
    }
  }

  function initializeAsync(force = false): Promise<void> {
    if (!force && status.value !== 'checking') {
      return Promise.resolve();
    }

    if (initializationPromise) {
      return initializationPromise;
    }

    status.value = 'checking';

    initializationPromise = loadSessionAsync().finally(() => {
      initializationPromise = null;
    });

    return initializationPromise;
  }

  return {
    status,
    user,
    isAuthenticated,
    initializeAsync,
  };
});

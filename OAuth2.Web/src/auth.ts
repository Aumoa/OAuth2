import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { HttpStatusCodeError } from './core/api/HttpStatusCodeError';

export type AuthStatus =
  | 'checking'
  | 'authenticated'
  | 'unauthenticated'
  | 'error';

export interface User {
  id: string;
  sub: string;
  email: string;
  profile: string;
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
        throw new HttpStatusCodeError(
          response.status,
          response.statusText,
        );
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

  async function logoutAsync(): Promise<void> {
    const response = await fetch('/api/v1/logout', {
      method: 'POST',
      credentials: 'include',
    });

    if (!response.ok) {
      throw new HttpStatusCodeError(
        response.status,
        response.statusText,
      );
    }

    setUnauthenticated();
  }

  return {
    status,
    user,
    isAuthenticated,
    initializeAsync,
    logoutAsync,
  };
});
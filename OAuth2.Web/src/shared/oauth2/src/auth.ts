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
  emailVerified?: boolean;
  name?: string;
  groups?: string[];
}

interface SessionClaims {
  sub: string;

  id?: string;
  preferred_username?: string;
  picture?: string;
  email?: string;
  emailVerified?: boolean;
  email_verified?: boolean;
  name?: string;
  groups?: unknown;
}

function readGroups(groups: unknown): string[] {
  if (!Array.isArray(groups)) {
    return [];
  }

  return [...new Set(groups.filter((group): group is string => (
    typeof group === 'string' && group.trim().length > 0
  )))];
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

      const claims = await response.json() as SessionClaims;
      user.value = {
        sub: claims.sub,
        id: claims.id ?? claims.preferred_username,
        picture: claims.picture,
        email: claims.email,
        emailVerified: claims.emailVerified ?? claims.email_verified,
        name: claims.name,
        groups: readGroups(claims.groups),
      };
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

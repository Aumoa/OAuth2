import type { AuthorizationRequest, LoginResponse } from './accounts.ts';
import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

const actionHeaders = {
  'X-OAuth2-Action': '1',
};

export interface RememberedAccount {
  accountKey: string;
  id: string;
  name?: string | null;
  email?: string | null;
  picture?: string | null;
  canSignIn: boolean;
  authenticatedAt: string;
}

export class Sessions {
  static async getRememberedAccountsAsync(): Promise<RememberedAccount[]> {
    const response = await fetch('/api/v1/session/accounts', {
      credentials: 'include',
      headers: {
        Accept: 'application/json',
      },
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as RememberedAccount[];
  }

  static async continueWithRememberedAccountAsync(
    accountKey: string,
    authorization: AuthorizationRequest,
  ): Promise<LoginResponse> {
    const response = await fetch(
      `/api/v1/session/accounts/${encodeURIComponent(accountKey)}/authorization-codes`,
      {
        method: 'POST',
        credentials: 'include',
        headers: {
          ...actionHeaders,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(authorization),
      },
    );
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    const login = await response.json() as LoginResponse;
    if (login.state !== 'authenticated' || !login.redirectUri) {
      throw new Error('Remembered sign-in response is invalid.');
    }

    const redirectUri = new URL(login.redirectUri, window.location.origin);
    if (
      redirectUri.origin !== window.location.origin
      || redirectUri.pathname !== '/'
      || !redirectUri.searchParams.get('code')
      || !redirectUri.searchParams.get('state')
    ) {
      throw new Error('Authorization redirect URI is invalid.');
    }

    window.location.assign(redirectUri.href);
    return login;
  }

  static async deleteRememberedAccountAsync(accountKey: string): Promise<void> {
    const response = await fetch(
      `/api/v1/session/accounts/${encodeURIComponent(accountKey)}`,
      {
        method: 'DELETE',
        credentials: 'include',
        headers: actionHeaders,
      },
    );
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }
  }

  static async deleteAsync(): Promise<void> {
    const response = await fetch('/api/v1/session', {
      method: 'DELETE',
      credentials: 'include',
      headers: actionHeaders,
    });

    if (!response.ok) {
      throw new HttpStatusCodeError(
        response.status,
        response.statusText,
      );
    }
  }
}

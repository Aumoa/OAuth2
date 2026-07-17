import {
  resolveAuthorizationRedirect,
  type AuthorizationRequest,
  type LoginResponse,
} from './accounts.ts';
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
    consentGranted: boolean,
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
        body: JSON.stringify({ authorization, consentGranted }),
      },
    );
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    const login = await response.json() as LoginResponse;
    if (login.state !== 'authenticated') {
      throw new Error('Remembered sign-in response is invalid.');
    }

    const redirectUri = resolveAuthorizationRedirect(login, authorization);

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

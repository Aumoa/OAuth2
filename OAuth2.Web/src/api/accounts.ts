import { HttpStatusCodeError } from '../core/src/http-status-code-error';
import { resolvePreferredLocale } from '../core/i18n/locale';
import { router } from '../router';

const pendingEmailVerificationSubKey = 'oauth2.pendingEmailVerificationSub';

export type LoginState = 'authenticated' | 'emailVerificationRequired';

export interface AuthorizationRequest {
  clientId: string;
  redirectUri: string;
  responseType: string;
  scope: string;
  state: string;
  nonce?: string | null;
  codeChallenge: string;
  codeChallengeMethod: string;
}

export interface LoginResponse {
  state: LoginState;
  sub?: string | null;
  redirectUri?: string | null;
}

export function resolveAuthorizationRedirect(
  login: LoginResponse,
  authorization: AuthorizationRequest,
): URL {
  if (!login.redirectUri) {
    throw new Error('Authorization redirect URI is missing.');
  }

  const actual = new URL(login.redirectUri, window.location.origin);
  const expected = new URL(authorization.redirectUri, window.location.origin);
  if (
    actual.origin !== expected.origin
    || actual.pathname !== expected.pathname
    || actual.hash !== expected.hash
  ) {
    throw new Error('Authorization redirect URI is invalid.');
  }

  const remainingParameters = [...actual.searchParams.entries()];
  for (const expectedParameter of expected.searchParams.entries()) {
    const index = remainingParameters.findIndex(
      ([name, value]) => name === expectedParameter[0] && value === expectedParameter[1],
    );
    if (index < 0) {
      throw new Error('Authorization redirect URI is invalid.');
    }

    remainingParameters.splice(index, 1);
  }

  const code = remainingParameters.filter(([name, value]) => name === 'code' && value);
  const state = remainingParameters.filter(
    ([name, value]) => name === 'state' && value === authorization.state,
  );
  if (remainingParameters.length !== 2 || code.length !== 1 || state.length !== 1) {
    throw new Error('Authorization redirect URI is invalid.');
  }

  return actual;
}

interface EmailVerificationChallenge {
  sub: string;
}

function jsonRequestHeaders(): HeadersInit {
  return {
    'Accept-Language': resolvePreferredLocale(),
    'Content-Type': 'application/json',
    'X-OAuth2-Action': '1',
  };
}

export class RegisterForm {
  id: string;
  password: string;
  fullName: string;
  email: string;

  constructor(id: string, password: string, fullName: string, email: string) {
    this.id = id;
    this.password = password;
    this.fullName = fullName;
    this.email = email;
  }

  verify(): void {
    if (this.id.trim() === '' || this.password.trim() === '' || this.fullName.trim() === '' || this.email.trim() === '') {
      throw new Error('Invalid arguments.');
    }
    
    const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/i;
    if (!emailPattern.test(this.email)) {
      throw new Error('Invalid arguments.');
    }
  }
}

export class Accounts {
  static async verifyAsync(id: string): Promise<boolean> {
    const response = await fetch(`/api/v1/accounts/${encodeURIComponent(id)}`, {
      method: 'HEAD',
    });
    if (response.status === 404) {
      return false;
    }

    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return true;
  }

  static async registerAsync(form: RegisterForm): Promise<void> {
    form.verify();

    const response = await fetch(`/api/v1/accounts`, {
      method: 'POST',
      headers: jsonRequestHeaders(),
      body: JSON.stringify(form),
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    const challenge = await response.json() as EmailVerificationChallenge;
    if (!challenge.sub) {
      throw new Error('Email verification challenge is missing.');
    }

    Accounts.setPendingEmailVerificationSub(challenge.sub);
    await router.replace('/verifyEmail');
  }

  static async loginAsync(
    id: string,
    password: string,
    authorization: AuthorizationRequest,
  ): Promise<LoginResponse> {
    const response = await fetch('/api/v1/authorization-codes', {
      method: 'POST',
      headers: jsonRequestHeaders(),
      credentials: 'include',
      body: JSON.stringify({ id, password, authorization }),
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    const login = await response.json() as LoginResponse;
    if (login.state === 'emailVerificationRequired') {
      if (!login.sub) {
        throw new Error('Email verification subject is missing.');
      }

      Accounts.setPendingEmailVerificationSub(login.sub);
      await router.replace('/verifyEmail');
    } else if (login.state === 'authenticated') {
      const redirectUri = resolveAuthorizationRedirect(login, authorization);

      Accounts.clearPendingEmailVerificationSub();
      window.location.assign(redirectUri.href);
    } else {
      throw new Error('Unknown login state.');
    }

    return login;
  }

  static async verifyEmailAsync(sub: string, code: string): Promise<void> {
    const response = await fetch('/api/v1/email-verifications', {
      method: 'PUT',
      headers: jsonRequestHeaders(),
      body: JSON.stringify({ sub, code }),
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    Accounts.clearPendingEmailVerificationSub();
  }

  static async resendEmailVerificationAsync(sub: string): Promise<void> {
    const response = await fetch('/api/v1/email-verification-deliveries', {
      method: 'POST',
      headers: jsonRequestHeaders(),
      body: JSON.stringify({ sub }),
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }
  }

  static getPendingEmailVerificationSub(): string | null {
    return sessionStorage.getItem(pendingEmailVerificationSubKey);
  }

  private static setPendingEmailVerificationSub(sub: string): void {
    sessionStorage.setItem(pendingEmailVerificationSubKey, sub);
  }

  private static clearPendingEmailVerificationSub(): void {
    sessionStorage.removeItem(pendingEmailVerificationSubKey);
  }
}

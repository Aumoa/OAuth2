import { HttpStatusCodeError } from '../core/api/HttpStatusCodeError';
import { resolvePreferredLocale } from '../core/i18n/locale';
import { router } from '../router';

const pendingEmailVerificationSubKey = 'oauth2.pendingEmailVerificationSub';

export type LoginState = 'authenticated' | 'emailVerificationRequired';

export interface LoginResponse {
  state: LoginState;
  sub?: string | null;
}

interface EmailVerificationChallenge {
  sub: string;
}

function jsonRequestHeaders(): HeadersInit {
  return {
    'Accept-Language': resolvePreferredLocale(),
    'Content-Type': 'application/json',
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
    const response = await fetch(`/api/v1/accounts/verify?id=${encodeURIComponent(id)}`);
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    const result = await response.json();
    return result as boolean;
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

  static async loginAsync(id: string, password: string): Promise<LoginResponse> {
    const response = await fetch('/api/v1/accounts/login', {
      method: 'POST',
      headers: jsonRequestHeaders(),
      body: JSON.stringify({ id, password }),
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
      Accounts.clearPendingEmailVerificationSub();
      await router.replace('/');
    } else {
      throw new Error('Unknown login state.');
    }

    return login;
  }

  static async verifyEmailAsync(sub: string, code: string): Promise<void> {
    const response = await fetch('/api/v1/accounts/email/verify', {
      method: 'POST',
      headers: jsonRequestHeaders(),
      body: JSON.stringify({ sub, code }),
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    Accounts.clearPendingEmailVerificationSub();
  }

  static async resendEmailVerificationAsync(sub: string): Promise<void> {
    const response = await fetch('/api/v1/accounts/email/resend', {
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

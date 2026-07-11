import { HttpStatusCodeError } from "../core/api/HttpStatusCodeError";
import { router } from "../router";

export type RegisterState = 'unexpected' | 'unauthorized' | 'authrozied';

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
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(form)
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    await router.replace('/verifyEmail');
  }
}

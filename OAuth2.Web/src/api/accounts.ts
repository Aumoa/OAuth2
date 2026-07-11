import { HttpStatusCodeError } from "../core/api/HttpStatusCodeError";
import { router } from "../router";

export class RegisterForm {
  id: string;
  password: string;
  fullname: string;
  email: string;

  constructor(id: string, password: string, fullname: string, email: string) {
    this.id = id;
    this.password = password;
    this.fullname = fullname;
    this.email = email;
  }

  verify() {
    if (this.id == '' || this.password == '' || this.fullname == '' || this.email == '') {
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
    const response = await fetch(`/api/v1/accounts/verify?id=${encodeURI(id)}`);
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    const result = await response.json();
    return result as boolean;
  }

  static async registerAsync(form: RegisterForm) {
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

    router.replace('/verifyEmail');
  }
};
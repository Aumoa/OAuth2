import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

export interface ApplicationSummary {
  id: string;
  name: string;
  createdAt: string;
}

export class Applications {
  static async listAsync(): Promise<ApplicationSummary[]> {
    const response = await fetch('/api/v1/applications', {
      credentials: 'include',
      headers: {
        Accept: 'application/json',
      },
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as ApplicationSummary[];
  }
}

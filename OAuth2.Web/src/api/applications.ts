import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

export interface ApplicationSummary {
  id: string;
  name: string;
  createdAt: string;
}

export class Applications {
  static async createAsync(clientId: string, name: string): Promise<ApplicationSummary> {
    const response = await fetch('/api/v1/applications', {
      method: 'POST',
      credentials: 'include',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        'X-OAuth2-Action': '1',
      },
      body: JSON.stringify({ clientId, name }),
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as ApplicationSummary;
  }

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

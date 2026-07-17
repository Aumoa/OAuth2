import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

export interface OrganizationSummary {
  id: string;
  name: string;
  role: string;
  createdAt: string;
}

export class Organizations {
  static async createAsync(id: string, name: string): Promise<OrganizationSummary> {
    const response = await fetch('/api/v1/organizations', {
      method: 'POST',
      credentials: 'include',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        'X-OAuth2-Action': '1',
      },
      body: JSON.stringify({ id, name }),
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as OrganizationSummary;
  }

  static async getAsync(id: string): Promise<OrganizationSummary> {
    const response = await fetch(`/api/v1/organizations/${encodeURIComponent(id)}`, {
      credentials: 'include',
      headers: {
        Accept: 'application/json',
      },
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as OrganizationSummary;
  }

  static async listAsync(): Promise<OrganizationSummary[]> {
    const response = await fetch('/api/v1/organizations', {
      credentials: 'include',
      headers: {
        Accept: 'application/json',
      },
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as OrganizationSummary[];
  }
}

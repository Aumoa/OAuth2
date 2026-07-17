import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

export type OAuthApplicationType = 'web' | 'android' | 'ios' | 'macos';

export interface ApplicationSummary {
  id: string;
  name: string;
  applicationType: OAuthApplicationType;
  createdAt: string;
}

export interface ApplicationDetails extends ApplicationSummary {
  redirectUris: string[];
  allowedScopes: string[];
}

export class Applications {
  static async createAsync(
    clientId: string,
    name: string,
    applicationType: OAuthApplicationType,
  ): Promise<ApplicationSummary> {
    const response = await fetch('/api/v1/applications', {
      method: 'POST',
      credentials: 'include',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        'X-OAuth2-Action': '1',
      },
      body: JSON.stringify({ clientId, name, applicationType }),
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as ApplicationSummary;
  }

  static async deleteAsync(clientId: string): Promise<void> {
    const response = await fetch(Applications.applicationUri(clientId), {
      method: 'DELETE',
      credentials: 'include',
      headers: {
        'X-OAuth2-Action': '1',
      },
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }
  }

  static async getAsync(clientId: string): Promise<ApplicationDetails> {
    const response = await fetch(Applications.applicationUri(clientId), {
      credentials: 'include',
      headers: {
        Accept: 'application/json',
      },
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as ApplicationDetails;
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

  static async updateAsync(
    clientId: string,
    redirectUris: string[],
    allowedScopes: string[],
  ): Promise<void> {
    const response = await fetch(Applications.applicationUri(clientId), {
      method: 'PUT',
      credentials: 'include',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        'X-OAuth2-Action': '1',
      },
      body: JSON.stringify({ redirectUris, allowedScopes }),
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }
  }

  private static applicationUri(clientId: string): string {
    return `/api/v1/applications/${encodeURIComponent(clientId)}`;
  }
}

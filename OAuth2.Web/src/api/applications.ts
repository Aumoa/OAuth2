import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

export interface ApplicationSummary {
  id: string;
  name: string;
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
    organizationId?: string,
  ): Promise<ApplicationSummary> {
    const response = await fetch(Applications.collectionUri(organizationId), {
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

  static async deleteAsync(clientId: string, organizationId?: string): Promise<void> {
    const response = await fetch(Applications.applicationUri(clientId, organizationId), {
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

  static async getAsync(clientId: string, organizationId?: string): Promise<ApplicationDetails> {
    const response = await fetch(Applications.applicationUri(clientId, organizationId), {
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

  static async listAsync(organizationId?: string): Promise<ApplicationSummary[]> {
    const response = await fetch(Applications.collectionUri(organizationId), {
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
    organizationId?: string,
  ): Promise<void> {
    const response = await fetch(Applications.applicationUri(clientId, organizationId), {
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

  private static collectionUri(organizationId?: string): string {
    const baseUri = '/api/v1/applications';
    return organizationId === undefined
      ? baseUri
      : `${baseUri}?organizationId=${encodeURIComponent(organizationId)}`;
  }

  private static applicationUri(clientId: string, organizationId?: string): string {
    const baseUri = `/api/v1/applications/${encodeURIComponent(clientId)}`;
    return organizationId === undefined
      ? baseUri
      : `${baseUri}?organizationId=${encodeURIComponent(organizationId)}`;
  }
}

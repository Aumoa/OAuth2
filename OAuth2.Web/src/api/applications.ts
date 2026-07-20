import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

export type OAuthApplicationType = 'web' | 'android' | 'ios' | 'desktop';

export type GroupClaimFormat = 'dash' | 'path' | 'colon';

export interface GroupClaimMapping {
  format: GroupClaimFormat;
  selectors: string[];
}

export interface ApplicationSummary {
  id: string;
  name: string;
  applicationType: OAuthApplicationType;
  createdAt: string;
}

export interface ApplicationDetails extends ApplicationSummary {
  requiresSecret: boolean;
  redirectUris: string[];
  allowedScopes: string[];
  groupClaimMapping: GroupClaimMapping | null;
}

export interface ApplicationSecretSummary {
  id: number;
  prefix: string;
  createdAt: string;
}

export interface CreatedApplicationSecret extends ApplicationSecretSummary {
  secret: string;
}

export class Applications {
  static async createSecretAsync(
    clientId: string,
    organizationId?: string,
  ): Promise<CreatedApplicationSecret> {
    const response = await fetch(Applications.applicationSecretsUri(clientId, organizationId), {
      method: 'POST',
      cache: 'no-store',
      credentials: 'include',
      headers: {
        Accept: 'application/json',
        'X-OAuth2-Action': '1',
      },
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as CreatedApplicationSecret;
  }

  static async createAsync(
    clientId: string,
    name: string,
    applicationType: OAuthApplicationType,
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
      body: JSON.stringify({ clientId, name, applicationType }),
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

  static async deleteSecretAsync(
    clientId: string,
    secretId: number,
    organizationId?: string,
  ): Promise<void> {
    const response = await fetch(
      Applications.applicationSecretUri(clientId, secretId, organizationId),
      {
        method: 'DELETE',
        credentials: 'include',
        headers: {
          'X-OAuth2-Action': '1',
        },
      },
    );
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

  static async listSecretsAsync(
    clientId: string,
    organizationId?: string,
  ): Promise<ApplicationSecretSummary[]> {
    const response = await fetch(Applications.applicationSecretsUri(clientId, organizationId), {
      cache: 'no-store',
      credentials: 'include',
      headers: {
        Accept: 'application/json',
      },
    });
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as ApplicationSecretSummary[];
  }

  static async updateAsync(
    clientId: string,
    redirectUris: string[],
    allowedScopes: string[],
    groupClaimMapping: GroupClaimMapping | null,
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
      body: JSON.stringify({ redirectUris, allowedScopes, groupClaimMapping }),
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

  private static applicationSecretsUri(clientId: string, organizationId?: string): string {
    const baseUri = `/api/v1/applications/${encodeURIComponent(clientId)}/secrets`;
    return organizationId === undefined
      ? baseUri
      : `${baseUri}?organizationId=${encodeURIComponent(organizationId)}`;
  }

  private static applicationSecretUri(
    clientId: string,
    secretId: number,
    organizationId?: string,
  ): string {
    const baseUri = `/api/v1/applications/${encodeURIComponent(clientId)}/secrets/${secretId}`;
    return organizationId === undefined
      ? baseUri
      : `${baseUri}?organizationId=${encodeURIComponent(organizationId)}`;
  }
}

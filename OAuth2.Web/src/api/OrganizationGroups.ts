import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';
import type { OrganizationMemberPage } from './Organizations.ts';

export interface OrganizationGroupSummary {
  organizationId: string;
  id: string;
  name: string;
  memberCount: number;
  createdAt: string;
}

const actionHeaders = {
  'Content-Type': 'application/json',
  'X-OAuth2-Action': '1',
};

export class OrganizationGroups {
  static async listAsync(organizationId: string): Promise<OrganizationGroupSummary[]> {
    const response = await fetch(this.collectionPath(organizationId), {
      credentials: 'include',
      headers: { Accept: 'application/json' },
    });
    return await this.readAsync<OrganizationGroupSummary[]>(response);
  }

  static async getAsync(
    organizationId: string,
    groupId: string,
  ): Promise<OrganizationGroupSummary> {
    const response = await fetch(this.groupPath(organizationId, groupId), {
      credentials: 'include',
      headers: { Accept: 'application/json' },
    });
    return await this.readAsync<OrganizationGroupSummary>(response);
  }

  static async createAsync(
    organizationId: string,
    id: string,
    name: string,
  ): Promise<OrganizationGroupSummary> {
    const response = await fetch(this.collectionPath(organizationId), {
      method: 'POST',
      credentials: 'include',
      headers: actionHeaders,
      body: JSON.stringify({ id, name }),
    });
    return await this.readAsync<OrganizationGroupSummary>(response);
  }

  static async getMembersAsync(
    organizationId: string,
    groupId: string,
    page: number,
    pageSize: number,
  ): Promise<OrganizationMemberPage> {
    const parameters = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    const response = await fetch(
      `${this.groupPath(organizationId, groupId)}/members?${parameters}`,
      {
        credentials: 'include',
        headers: { Accept: 'application/json' },
      },
    );
    return await this.readAsync<OrganizationMemberPage>(response);
  }

  static async addMemberAsync(
    organizationId: string,
    groupId: string,
    accountId: string,
  ): Promise<void> {
    const response = await fetch(`${this.groupPath(organizationId, groupId)}/members`, {
      method: 'POST',
      credentials: 'include',
      headers: actionHeaders,
      body: JSON.stringify({ accountId }),
    });
    this.ensureSuccess(response);
  }

  static async deleteMemberAsync(
    organizationId: string,
    groupId: string,
    accountId: string,
  ): Promise<void> {
    const response = await fetch(
      `${this.groupPath(organizationId, groupId)}/members/${encodeURIComponent(accountId)}`,
      {
        method: 'DELETE',
        credentials: 'include',
        headers: { 'X-OAuth2-Action': '1' },
      },
    );
    this.ensureSuccess(response);
  }

  private static collectionPath(organizationId: string): string {
    return `/api/v1/organizations/${encodeURIComponent(organizationId)}/groups`;
  }

  private static groupPath(organizationId: string, groupId: string): string {
    return `${this.collectionPath(organizationId)}/${encodeURIComponent(groupId)}`;
  }

  private static ensureSuccess(response: Response): void {
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }
  }

  private static async readAsync<T>(response: Response): Promise<T> {
    this.ensureSuccess(response);
    return await response.json() as T;
  }
}

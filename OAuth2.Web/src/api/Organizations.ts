import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

export interface OrganizationSummary {
  id: string;
  name: string;
  role: OrganizationRole;
  createdAt: string;
}

export type OrganizationRole = 'owner' | 'admin' | 'member';

export interface OrganizationMemberSummary {
  accountId: string;
  name: string;
  role: OrganizationRole;
  joinedAt: string;
}

export interface OrganizationMemberPage {
  items: OrganizationMemberSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

const actionHeaders = {
  'Content-Type': 'application/json',
  'X-OAuth2-Action': '1',
};

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

  static async getMembersAsync(
    id: string,
    page: number,
    pageSize: number,
  ): Promise<OrganizationMemberPage> {
    const parameters = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    const response = await fetch(
      `/api/v1/organizations/${encodeURIComponent(id)}/members?${parameters}`,
      {
        credentials: 'include',
        headers: {
          Accept: 'application/json',
        },
      },
    );
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }

    return await response.json() as OrganizationMemberPage;
  }

  static async addMemberAsync(
    id: string,
    accountId: string,
    role: OrganizationRole,
  ): Promise<void> {
    const response = await fetch(
      `/api/v1/organizations/${encodeURIComponent(id)}/members`,
      {
        method: 'POST',
        credentials: 'include',
        headers: actionHeaders,
        body: JSON.stringify({ accountId, role }),
      },
    );
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }
  }

  static async updateMemberAsync(
    id: string,
    accountId: string,
    role: OrganizationRole,
  ): Promise<void> {
    const response = await fetch(
      `/api/v1/organizations/${encodeURIComponent(id)}/members/${encodeURIComponent(accountId)}`,
      {
        method: 'PUT',
        credentials: 'include',
        headers: actionHeaders,
        body: JSON.stringify({ role }),
      },
    );
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }
  }

  static async deleteMemberAsync(id: string, accountId: string): Promise<void> {
    const response = await fetch(
      `/api/v1/organizations/${encodeURIComponent(id)}/members/${encodeURIComponent(accountId)}`,
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

  static async deleteAsync(id: string, name: string): Promise<void> {
    const response = await fetch(
      `/api/v1/organizations/${encodeURIComponent(id)}`,
      {
        method: 'DELETE',
        credentials: 'include',
        headers: actionHeaders,
        body: JSON.stringify({ name }),
      },
    );
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }
  }

  static async transferOwnershipAsync(id: string, accountId: string): Promise<void> {
    const response = await fetch(
      `/api/v1/organizations/${encodeURIComponent(id)}/owner`,
      {
        method: 'PUT',
        credentials: 'include',
        headers: actionHeaders,
        body: JSON.stringify({ accountId }),
      },
    );
    if (!response.ok) {
      throw new HttpStatusCodeError(response.status, response.statusText);
    }
  }
}

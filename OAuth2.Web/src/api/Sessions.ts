import { HttpStatusCodeError } from '../core/src/http-status-code-error.ts';

export class Sessions {
  static async deleteAsync(): Promise<void> {
    const response = await fetch('/api/v1/session', {
      method: 'DELETE',
      credentials: 'include',
    });

    if (!response.ok) {
      throw new HttpStatusCodeError(
        response.status,
        response.statusText,
      );
    }
  }
}

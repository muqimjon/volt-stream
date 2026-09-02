import { HttpContextToken } from '@angular/common/http';

export const SILENT = new HttpContextToken<boolean>(() => false);

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly payload?: unknown,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

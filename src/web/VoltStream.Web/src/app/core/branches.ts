import { Injectable, inject, signal } from '@angular/core';
import { ApiClient } from './api/api-client';
import { AuthService } from './auth/auth';
import { Branch } from './models';

@Injectable({ providedIn: 'root' })
export class BranchStore {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthService);
  private loading: Promise<void> | null = null;

  readonly branches = signal<Branch[]>([]);

  load(): Promise<void> {
    if (!this.auth.isAdmin()) return Promise.resolve();
    this.loading ??= this.api
      .get<Branch[]>('branches', undefined, true)
      .then((list) => this.branches.set(list.filter((b) => b.isActive)))
      .catch(() => undefined)
      .then(() => undefined);
    return this.loading;
  }

  nameOf(id: number | null | undefined): string | null {
    if (id === null || id === undefined) return null;
    return this.branches().find((b) => b.id === id)?.name ?? null;
  }
}

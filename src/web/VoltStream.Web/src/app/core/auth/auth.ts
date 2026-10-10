import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LoginResult } from '../models';

const SESSION_KEY = 'vs.session';
const BRANCH_KEY = 'vs.branch';

export interface Session extends LoginResult {
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly session = signal<Session | null>(readSession());
  readonly selectedBranchId = signal<number | null>(readBranch());

  readonly token = computed(() => this.session()?.token ?? null);
  readonly permissions = computed(() => this.session()?.permissions ?? []);
  readonly isAdmin = computed(() => {
    const s = this.session();
    if (!s) return false;
    return (s.role ?? '').toLowerCase() === 'admin' || s.permissions.includes('branches.all');
  });
  readonly isAuthenticated = computed(() => !!this.session() && !isExpired(this.session()!));
  readonly branchId = computed(() => (this.isAdmin() ? this.selectedBranchId() : null));
  readonly branchName = computed(() => this.session()?.branchName ?? null);
  readonly printPolicy = computed(() => this.session()?.printPolicy ?? 'Ask');

  async login(username: string, password: string): Promise<void> {
    const result = await firstValueFrom(
      this.http.post<LoginResult>('/api/user/login', { username, password }),
    );
    this.session.set({ ...result, exp: expOf(result.token) });
    localStorage.setItem(SESSION_KEY, JSON.stringify(this.session()));
  }

  logout(): void {
    this.session.set(null);
    localStorage.removeItem(SESSION_KEY);
  }

  selectBranch(id: number | null): void {
    this.selectedBranchId.set(id);
    if (id === null) localStorage.removeItem(BRANCH_KEY);
    else localStorage.setItem(BRANCH_KEY, String(id));
  }

  checkExpiry(): boolean {
    const s = this.session();
    if (s && isExpired(s)) {
      this.logout();
      return false;
    }
    return !!s;
  }
}

function isExpired(s: Session): boolean {
  return !s.exp || s.exp * 1000 <= Date.now();
}

function expOf(token: string): number {
  try {
    const payload = token.split('.')[1];
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return Number(JSON.parse(json).exp) || 0;
  } catch {
    return 0;
  }
}

function readSession(): Session | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const s = JSON.parse(raw) as Session;
    if (!s?.token || isExpired(s)) {
      localStorage.removeItem(SESSION_KEY);
      return null;
    }
    return s;
  } catch {
    return null;
  }
}

function readBranch(): number | null {
  const raw = localStorage.getItem(BRANCH_KEY);
  const n = raw ? Number(raw) : NaN;
  return Number.isFinite(n) ? n : null;
}

import { Injectable, computed, inject, signal } from '@angular/core';
import { PermissionService } from '../../core/permissions/permission';
import { NAV_ITEMS } from './nav';

const KEY = 'vs.bottomNav';
export const MAX_PINNED = 4;

function read(): string[] | null {
  try {
    const parsed = JSON.parse(localStorage.getItem(KEY) ?? 'null');
    return Array.isArray(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class BottomNavService {
  private readonly permissions = inject(PermissionService);
  private readonly chosen = signal<string[] | null>(read());

  readonly allowed = computed(() =>
    NAV_ITEMS.filter((i) => !i.permission || this.permissions.has(i.permission)),
  );
  readonly pinned = computed(() => {
    const items = this.allowed();
    if (items.length <= MAX_PINNED) return items;
    const chosen = this.chosen();
    const picked = chosen ? items.filter((i) => chosen.includes(i.path)).slice(0, MAX_PINNED) : [];
    return picked.length ? picked : items.slice(0, MAX_PINNED);
  });
  readonly more = computed(() => {
    const pinned = new Set(this.pinned().map((i) => i.path));
    return this.allowed().filter((i) => !pinned.has(i.path));
  });

  isPinned(path: string): boolean {
    return this.pinned().some((i) => i.path === path);
  }

  toggle(path: string): void {
    const current = this.pinned().map((i) => i.path);
    const next = current.includes(path) ? current.filter((p) => p !== path) : [...current, path];
    if (!next.length || next.length > MAX_PINNED) return;
    this.chosen.set(next);
    try {
      localStorage.setItem(KEY, JSON.stringify(next));
    } catch {}
  }
}

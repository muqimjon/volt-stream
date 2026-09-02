import { Component, inject, signal } from '@angular/core';
import { ApiClient } from '../core/api/api-client';
import { AuthService } from '../core/auth/auth';
import { TranslatePipe } from '../core/i18n/translate';
import { Branch } from '../core/models';

@Component({
  selector: 'app-branch-switcher',
  imports: [TranslatePipe],
  template: `
    @if (auth.isAdmin()) {
      <select class="pill" [value]="auth.selectedBranchId() ?? ''" (change)="pick($any($event.target).value)">
        <option value="">{{ 'Web.AllBranches' | t }}</option>
        @for (branch of branches(); track branch.id) {
          <option [value]="branch.id">{{ branch.name }}</option>
        }
      </select>
    } @else if (auth.branchName()) {
      <span class="pill">{{ auth.branchName() }}</span>
    }
  `,
  styles: `
    .pill {
      max-width: 46vw;
      border: none;
      border-radius: var(--r-pill);
      background: var(--brand-subtle);
      color: var(--brand);
      font-size: var(--fs-caption);
      font-weight: 600;
      padding: 6px 12px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    select.pill {
      min-height: 32px;
    }
  `,
})
export class BranchSwitcher {
  protected readonly auth = inject(AuthService);
  private readonly api = inject(ApiClient);
  protected readonly branches = signal<Branch[]>([]);

  constructor() {
    if (this.auth.isAdmin())
      this.api
        .get<Branch[]>('branches', undefined, true)
        .then((list) => this.branches.set(list.filter((b) => b.isActive)))
        .catch(() => undefined);
  }

  protected pick(value: string): void {
    this.auth.selectBranch(value ? Number(value) : null);
  }
}

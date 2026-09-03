import { Component, inject } from '@angular/core';
import { AuthService } from '../core/auth/auth';
import { BranchStore } from '../core/branches';
import { TranslatePipe } from '../core/i18n/translate';

@Component({
  selector: 'app-branch-switcher',
  imports: [TranslatePipe],
  template: `
    @if (auth.isAdmin()) {
      <select
        class="pill"
        [value]="auth.selectedBranchId() ?? ''"
        (change)="pick($any($event.target).value)"
      >
        <option value="">{{ 'Web.AllBranches' | t }}</option>
        @for (branch of store.branches(); track branch.id) {
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
  protected readonly store = inject(BranchStore);

  constructor() {
    void this.store.load();
  }

  protected pick(value: string): void {
    this.auth.selectBranch(value ? Number(value) : null);
  }
}

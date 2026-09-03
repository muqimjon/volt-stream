import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { BranchStore } from '../../core/branches';
import { TranslatePipe } from '../../core/i18n/translate';
import { SupplyListItem } from '../../core/models';
import { fmtDateTime, toDateInput } from '../../core/utils/date';
import { fmtNum } from '../../core/utils/number';
import { DateField } from '../../shared/date-field';
import { EmptyState } from '../../shared/empty-state';
import { Icon } from '../../shared/icon';
import { PageHeader } from '../../shared/page-header';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-my-supplies',
  imports: [TranslatePipe, DateField, PageHeader, EmptyState, Icon],
  templateUrl: './my-supplies.html',
  styleUrl: '../my-sales/my-sales.css',
})
export class MySuppliesPage {
  private readonly api = inject(ApiClient);
  protected readonly auth = inject(AuthService);
  protected readonly branches = inject(BranchStore);

  protected readonly fmt = fmtNum;
  protected readonly fmtDateTime = fmtDateTime;

  protected readonly showBranch = computed(
    () => this.auth.isAdmin() && this.auth.selectedBranchId() === null,
  );

  protected readonly date = signal(toDateInput(new Date()));
  protected readonly supplies = signal<SupplyListItem[]>([]);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(1);
  protected readonly loading = signal(false);

  constructor() {
    void this.branches.load();
    effect(() => {
      this.auth.branchId();
      untracked(() => void this.load(1));
    });
  }

  protected onDate(value: string): void {
    this.date.set(value);
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    if (this.loading()) return;
    this.loading.set(true);
    try {
      const result = await this.api.filter<SupplyListItem>(
        'supplies/filter',
        {
          filters: { date: [this.date()], product: ['include:category'] },
          page,
          pageSize: PAGE_SIZE,
          sortBy: 'Date',
          descending: true,
        },
        { branchScoped: true },
      );
      this.page.set(page);
      this.totalPages.set(result.paging?.totalPages ?? 1);
      this.supplies.set(page === 1 ? result.items : [...this.supplies(), ...result.items]);
    } catch {
      /* keep the current list */
    } finally {
      this.loading.set(false);
    }
  }

  protected loadMore(): void {
    void this.load(this.page() + 1);
  }
}

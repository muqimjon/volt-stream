import { Component, DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { BranchStore } from '../../core/branches';
import { TranslatePipe } from '../../core/i18n/translate';
import { PrintJobStatus, SaleListItem } from '../../core/models';
import { HasPermissionDirective, PermissionService } from '../../core/permissions/permission';
import { PwaService } from '../../core/pwa/pwa';
import { PrintService } from '../../core/realtime/print';
import { PrintHubService } from '../../core/realtime/print-hub';
import { fmtDateTime, toDateInput } from '../../core/utils/date';
import { fmtNum } from '../../core/utils/number';
import { DateField } from '../../shared/date-field';
import { EmptyState } from '../../shared/empty-state';
import { Icon } from '../../shared/icon';
import { PageHeader } from '../../shared/page-header';
import { StatusBadge } from '../../shared/status-badge';

const REFRESH_MS = 30000;
const PAGE_SIZE = 20;

@Component({
  selector: 'app-my-sales',
  imports: [
    TranslatePipe,
    DateField,
    PageHeader,
    EmptyState,
    StatusBadge,
    Icon,
    HasPermissionDirective,
  ],
  templateUrl: './my-sales.html',
  styleUrl: './my-sales.css',
})
export class MySalesPage {
  private readonly api = inject(ApiClient);
  private readonly hub = inject(PrintHubService);
  protected readonly auth = inject(AuthService);
  protected readonly branches = inject(BranchStore);
  private readonly permissions = inject(PermissionService);

  protected readonly print = inject(PrintService);
  protected readonly pwa = inject(PwaService);
  protected readonly fmt = fmtNum;
  protected readonly fmtDateTime = fmtDateTime;

  protected readonly showBranch = computed(
    () => this.auth.isAdmin() && this.auth.selectedBranchId() === null,
  );

  protected readonly date = signal(toDateInput(new Date()));
  protected readonly sales = signal<SaleListItem[]>([]);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(1);
  protected readonly loading = signal(false);

  protected readonly canPrint = computed(
    () => this.permissions.has('print.request') && this.auth.printPolicy() !== 'Deny',
  );

  constructor() {
    void this.branches.load();
    effect(() => {
      this.auth.branchId();
      untracked(() => void this.load(1));
    });
    const timer = setInterval(() => void this.load(1), REFRESH_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected statusOf(sale: SaleListItem): PrintJobStatus | null {
    return this.hub.jobFor(sale.id)?.status ?? sale.printJob?.status ?? null;
  }

  protected onDate(value: string): void {
    this.date.set(value);
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    if (this.loading()) return;
    this.loading.set(true);
    try {
      const result = await this.api.filter<SaleListItem>(
        'sales/filter',
        {
          filters: { date: [this.date()], customer: ['include'], items: ['include'] },
          page,
          pageSize: PAGE_SIZE,
          sortBy: 'Date',
          descending: true,
        },
        { silent: true, branchScoped: true },
      );
      this.page.set(page);
      this.totalPages.set(result.paging?.totalPages ?? 1);
      this.sales.set(page === 1 ? result.items : [...this.sales(), ...result.items]);
      for (const sale of result.items)
        this.hub.track(sale.printJob ? { ...sale.printJob, saleId: sale.id } : null);
    } catch {
      /* keep the current list */
    } finally {
      this.loading.set(false);
    }
  }

  protected loadMore(): void {
    void this.load(this.page() + 1);
  }

  protected requestPrint(sale: SaleListItem): void {
    void this.print.request(sale.id);
  }
}

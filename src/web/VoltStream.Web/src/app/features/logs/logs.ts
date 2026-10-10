import { Component, effect, inject, signal, untracked } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { TranslatePipe } from '../../core/i18n/translate';
import { AuditLog } from '../../core/models';
import { BreakpointService } from '../../core/ui/breakpoint';
import { fmtDateTime, toDateInput } from '../../core/utils/date';
import { DateField } from '../../shared/date-field';
import { EmptyState } from '../../shared/empty-state';
import { Icon } from '../../shared/icon';
import { PageHeader } from '../../shared/page-header';

const PAGE_SIZE = 25;

@Component({
  selector: 'app-logs',
  imports: [TranslatePipe, DateField, PageHeader, EmptyState, Icon],
  templateUrl: './logs.html',
  styleUrl: './logs.css',
})
export class LogsPage {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthService);
  protected readonly bp = inject(BreakpointService);
  protected readonly fmtDateTime = fmtDateTime;

  protected readonly date = signal(toDateInput(new Date()));
  protected readonly search = signal('');
  protected readonly logs = signal<AuditLog[]>([]);
  protected readonly page = signal(1);
  protected readonly totalPages = signal(1);
  protected readonly loading = signal(false);
  protected readonly expanded = signal<number | null>(null);

  constructor() {
    effect(() => {
      this.auth.branchId();
      untracked(() => void this.load(1));
    });
  }

  protected onDate(value: string): void {
    this.date.set(value);
    void this.load(1);
  }

  protected toggle(id: number): void {
    this.expanded.set(this.expanded() === id ? null : id);
  }

  protected async load(page: number): Promise<void> {
    if (this.loading()) return;
    this.loading.set(true);
    try {
      const result = await this.api.filter<AuditLog>(
        'audit-logs/filter',
        {
          filters: { timestamp: [this.date()] },
          search: this.search().trim() || null,
          page,
          pageSize: PAGE_SIZE,
          sortBy: 'Timestamp',
          descending: true,
        },
        { branchScoped: true },
      );
      this.page.set(page);
      this.totalPages.set(result.paging?.totalPages ?? 1);
      this.logs.set(page === 1 ? result.items : [...this.logs(), ...result.items]);
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

import { Component, DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { TranslatePipe } from '../../core/i18n/translate';
import { BranchStats } from '../../core/models';
import { PermissionService } from '../../core/permissions/permission';
import { PageTitle } from '../../core/ui/page-title';
import { addDays, startOfDay, toIsoOffset } from '../../core/utils/date';
import { fmtNum } from '../../core/utils/number';
import { EmptyState } from '../../shared/empty-state';
import { Icon } from '../../shared/icon';
import { PageHeader } from '../../shared/page-header';

type Range = 'today' | 'week' | 'month';
type Field = Exclude<keyof BranchStats, 'isOwnScope'>;

interface Section {
  title: string;
  icon: string;
  permission: string[];
  tiles: { key: string; field: Field; frac: number }[];
}

const SECTIONS: Section[] = [
  {
    title: 'Nav.Sales',
    icon: 'sale',
    permission: ['sales.create', 'sales.view'],
    tiles: [
      { key: 'Web.SalesCount', field: 'salesCount', frac: 0 },
      { key: 'Web.SoldRolls', field: 'soldRolls', frac: 0 },
      { key: 'Web.SoldLength', field: 'soldLength', frac: 2 },
      { key: 'Web.SoldWeight', field: 'soldWeight', frac: 3 },
    ],
  },
  {
    title: 'Web.Supply',
    icon: 'supply',
    permission: ['supplies.create', 'supplies.view'],
    tiles: [
      { key: 'Web.SuppliesCount', field: 'suppliesCount', frac: 0 },
      { key: 'Web.ReceivedRolls', field: 'receivedRolls', frac: 0 },
      { key: 'Web.ReceivedLength', field: 'receivedLength', frac: 2 },
      { key: 'Web.ReceivedWeight', field: 'receivedWeight', frac: 3 },
    ],
  },
  {
    title: 'Web.Drafts',
    icon: 'truck',
    permission: ['drafts.manage', 'drafts.confirm'],
    tiles: [
      { key: 'Web.DraftsCount', field: 'draftsCount', frac: 0 },
      { key: 'Web.DraftsOpen', field: 'draftsOpen', frac: 0 },
      { key: 'Web.DraftsConfirmed', field: 'draftsConfirmed', frac: 0 },
      { key: 'Web.DraftsCancelled', field: 'draftsCancelled', frac: 0 },
      { key: 'Web.DraftRolls', field: 'draftRolls', frac: 0 },
      { key: 'Web.DraftLength', field: 'draftLength', frac: 2 },
    ],
  },
];

@Component({
  selector: 'app-stats',
  imports: [TranslatePipe, PageHeader, EmptyState, Icon],
  templateUrl: './stats.html',
  styleUrl: './stats.css',
})
export class StatsPage {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthService);
  private readonly permissions = inject(PermissionService);
  private readonly pageTitle = inject(PageTitle);

  protected readonly fmt = fmtNum;
  protected readonly ranges: Range[] = ['today', 'week', 'month'];
  protected readonly rangeKey: Record<Range, string> = {
    today: 'Web.Today',
    week: 'Web.Week',
    month: 'Web.Month',
  };

  protected readonly range = signal<Range>('today');
  protected readonly stats = signal<BranchStats | null>(null);
  protected readonly loading = signal(false);

  protected readonly sections = computed(() =>
    SECTIONS.filter((s) => this.permissions.has(s.permission)),
  );
  protected readonly heading = computed(() => {
    const stats = this.stats();
    if (!stats) return 'Web.Stats';
    return stats.isOwnScope ? 'Web.StatsOwn' : 'Web.StatsBranch';
  });

  constructor() {
    effect(() => this.pageTitle.override.set(this.heading()));
    inject(DestroyRef).onDestroy(() => this.pageTitle.override.set(null));
    effect(() => {
      this.auth.branchId();
      untracked(() => void this.load());
    });
  }

  protected select(range: Range): void {
    this.range.set(range);
    void this.load();
  }

  protected valueOf(field: Field): number {
    return this.stats()?.[field] ?? 0;
  }

  private async load(): Promise<void> {
    const now = new Date();
    const today = startOfDay(now);
    const begin =
      this.range() === 'today'
        ? today
        : this.range() === 'week'
          ? addDays(today, -6)
          : new Date(today.getFullYear(), today.getMonth(), 1);
    const end = addDays(today, 1);

    this.loading.set(true);
    try {
      this.stats.set(
        await this.api.get<BranchStats>('stats/branch', {
          begin: toIsoOffset(begin),
          end: toIsoOffset(end),
          branchId: this.auth.branchId(),
        }),
      );
    } catch {
      this.stats.set(null);
    } finally {
      this.loading.set(false);
    }
  }
}

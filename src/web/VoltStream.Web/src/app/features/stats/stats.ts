import { Component, effect, inject, signal, untracked } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { TranslatePipe } from '../../core/i18n/translate';
import { BranchStats } from '../../core/models';
import { addDays, startOfDay, toIsoOffset } from '../../core/utils/date';
import { fmtNum } from '../../core/utils/number';
import { PageHeader } from '../../shared/page-header';

type Range = 'today' | 'week' | 'month';

const TILES: { key: string; field: keyof BranchStats; frac: number }[] = [
  { key: 'Web.SalesCount', field: 'salesCount', frac: 0 },
  { key: 'Web.SoldRolls', field: 'soldRolls', frac: 0 },
  { key: 'Web.SoldLength', field: 'soldLength', frac: 2 },
  { key: 'Web.SoldWeight', field: 'soldWeight', frac: 3 },
  { key: 'Web.SuppliesCount', field: 'suppliesCount', frac: 0 },
  { key: 'Web.ReceivedRolls', field: 'receivedRolls', frac: 0 },
  { key: 'Web.ReceivedLength', field: 'receivedLength', frac: 2 },
  { key: 'Web.ReceivedWeight', field: 'receivedWeight', frac: 3 },
];

@Component({
  selector: 'app-stats',
  imports: [TranslatePipe, PageHeader],
  templateUrl: './stats.html',
  styleUrl: './stats.css',
})
export class StatsPage {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthService);

  protected readonly tiles = TILES;
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

  constructor() {
    effect(() => {
      this.auth.branchId();
      untracked(() => void this.load());
    });
  }

  protected select(range: Range): void {
    this.range.set(range);
    void this.load();
  }

  protected valueOf(field: keyof BranchStats): number {
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

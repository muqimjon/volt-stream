import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { TranslatePipe, TranslateService } from '../../core/i18n/translate';
import { SaleDraft, SaleDraftItem } from '../../core/models';
import { PermissionService } from '../../core/permissions/permission';
import { PwaService } from '../../core/pwa/pwa';
import { PrintService } from '../../core/realtime/print';
import { PrintHubService } from '../../core/realtime/print-hub';
import { BreakpointService } from '../../core/ui/breakpoint';
import { DialogService } from '../../core/ui/dialog';
import { ToastService } from '../../core/ui/toast';
import { fmtDateTime } from '../../core/utils/date';
import { fmtNum } from '../../core/utils/number';
import { EmptyState } from '../../shared/empty-state';
import { Icon } from '../../shared/icon';
import { PageHeader } from '../../shared/page-header';
import { StatusBadge } from '../../shared/status-badge';

@Component({
  selector: 'app-drafts',
  imports: [TranslatePipe, RouterLink, PageHeader, EmptyState, StatusBadge, Icon],
  templateUrl: './drafts.html',
  styleUrl: '../my-sales/my-sales.css',
})
export class DraftsPage {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(DialogService);
  private readonly hub = inject(PrintHubService);
  private readonly permissions = inject(PermissionService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  protected readonly print = inject(PrintService);
  protected readonly pwa = inject(PwaService);
  protected readonly bp = inject(BreakpointService);
  protected readonly fmt = fmtNum;
  protected readonly fmtDateTime = fmtDateTime;

  protected readonly drafts = signal<SaleDraft[]>([]);
  protected readonly loading = signal(false);
  protected readonly expandedId = signal<number | null>(null);

  protected readonly canCreate = computed(() => this.permissions.has('drafts.manage'));
  protected readonly canPrint = computed(
    () => this.permissions.has('drafts.print') && this.auth.printPolicy() !== 'Deny',
  );
  protected readonly showBranch = computed(
    () => this.auth.isAdmin() && this.auth.selectedBranchId() === null,
  );

  constructor() {
    effect(() => {
      this.auth.branchId();
      untracked(() => void this.load());
    });
  }

  protected async load(): Promise<void> {
    if (this.loading()) return;
    this.loading.set(true);
    try {
      const branchId = this.auth.branchId();
      this.drafts.set(
        await this.api.get<SaleDraft[]>(
          'sale-drafts',
          branchId === null ? undefined : { branchId },
        ),
      );
    } catch {
      /* reported by the error interceptor */
    } finally {
      this.loading.set(false);
    }
  }

  protected canEdit(draft: SaleDraft): boolean {
    return (
      this.permissions.has('drafts.confirm') ||
      (this.permissions.has('drafts.manage') && draft.createdByUserId === this.auth.session()?.id)
    );
  }

  protected statusOf(draft: SaleDraft) {
    return this.hub.jobFor(draft.id, 'draft')?.status ?? null;
  }

  protected toggle(draft: SaleDraft): void {
    this.expandedId.set(this.expandedId() === draft.id ? null : draft.id);
  }

  protected itemLine(item: SaleDraftItem): string {
    return `${item.composition} = ${fmtNum(item.totalLength)} ${item.unit}`;
  }

  protected requestPrint(draft: SaleDraft): void {
    void this.print.request(draft.id, draft.branchId, 'draft');
  }

  protected async cancel(draft: SaleDraft): Promise<void> {
    const ok = await this.dialog.confirm(this.translate.t('Web.DraftCancelConfirm'), {
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.post<boolean>(`sale-drafts/${draft.id}/cancel`, {});
      this.toast.success(this.translate.t('Web.DraftCancelled'));
    } catch {
      /* reported by the error interceptor */
    }
    await this.load();
  }
}

import { Injectable, effect, inject } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { ApiError } from '../api/api-error';
import { AuthService } from '../auth/auth';
import { BranchStore } from '../branches';
import { TranslateService } from '../i18n/translate';
import { PrintJob } from '../models';
import { DialogService } from '../ui/dialog';
import { ToastService } from '../ui/toast';
import { PrintHubService, PrintTarget } from './print-hub';

const POLL_MS = 5000;

@Injectable({ providedIn: 'root' })
export class PrintService {
  private readonly api = inject(ApiClient);
  private readonly hub = inject(PrintHubService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly auth = inject(AuthService);
  private readonly branches = inject(BranchStore);
  private readonly dialog = inject(DialogService);
  private timer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    effect(() => {
      this.hub.byTarget();
      this.sync();
    });
  }

  isBusy(id: number, target: PrintTarget = 'sale'): boolean {
    const status = this.hub.jobFor(id, target)?.status;
    return status === 'Pending' || status === 'Printing';
  }

  async request(
    id: number,
    sourceBranchId: number | null = null,
    target: PrintTarget = 'sale',
    copies = 1,
  ): Promise<void> {
    const branchId = await this.pickBranch(sourceBranchId);
    if (branchId === undefined) return;
    try {
      const source = target === 'sale' ? { saleId: id } : { saleDraftId: id };
      const job = await this.api.post<PrintJob>(
        'print-jobs',
        { ...source, copies, branchId },
        true,
      );
      this.hub.track(job);
      this.toast.success(this.translate.t('Web.PrintRequested'));
      this.sync();
    } catch (e) {
      const err = e as ApiError;
      if (err.status === 409) this.toast.warn(this.translate.t('Web.PrintAlreadyPending'));
      else if (err.status === 403) this.toast.error(this.translate.t('Web.PrintDenied'));
      else this.toast.error(err.message);
    }
  }

  private async pickBranch(sourceBranchId: number | null): Promise<number | null | undefined> {
    if (!this.auth.isAdmin()) return null;
    await this.branches.load();
    const list = this.branches.branches();
    if (list.length < 2) return null;
    const chosen = await this.dialog.choose(
      'Web.PrintBranchTitle',
      list.map((b) => ({ id: b.id, label: b.name })),
      sourceBranchId,
    );
    return chosen === null ? undefined : chosen;
  }

  private active(): PrintJob[] {
    return Object.values(this.hub.byTarget()).filter(
      (j) => j.status === 'Pending' || j.status === 'Printing',
    );
  }

  private sync(): void {
    const needed = this.active().length > 0;
    if (needed && !this.timer) this.timer = setInterval(() => void this.tick(), POLL_MS);
    else if (!needed && this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  private async tick(): Promise<void> {
    if (this.hub.connected()) return;
    for (const job of this.active()) {
      try {
        this.hub.track(await this.api.get<PrintJob>(`print-jobs/${job.id}`, undefined, true));
      } catch {
        /* keep polling */
      }
    }
  }
}

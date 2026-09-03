import { Injectable, effect, inject } from '@angular/core';
import { ApiClient } from '../api/api-client';
import { ApiError } from '../api/api-error';
import { TranslateService } from '../i18n/translate';
import { PrintJob } from '../models';
import { ToastService } from '../ui/toast';
import { PrintHubService } from './print-hub';

const POLL_MS = 5000;

@Injectable({ providedIn: 'root' })
export class PrintService {
  private readonly api = inject(ApiClient);
  private readonly hub = inject(PrintHubService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private timer: ReturnType<typeof setInterval> | null = null;

  constructor() {
    effect(() => {
      this.hub.bySale();
      this.sync();
    });
  }

  isBusy(saleId: number): boolean {
    const status = this.hub.jobFor(saleId)?.status;
    return status === 'Pending' || status === 'Printing';
  }

  async request(saleId: number, copies = 1): Promise<void> {
    try {
      const job = await this.api.post<PrintJob>('print-jobs', { saleId, copies }, true);
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

  private active(): PrintJob[] {
    return Object.values(this.hub.bySale()).filter(
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

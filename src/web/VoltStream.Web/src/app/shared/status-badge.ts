import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '../core/i18n/translate';
import { PrintJobStatus } from '../core/models';

const TONE: Record<PrintJobStatus, string> = {
  Pending: 'warning',
  Printing: 'info',
  Completed: 'success',
  Failed: 'danger',
  Rejected: 'danger',
  Cancelled: 'muted',
};

@Component({
  selector: 'app-status-badge',
  imports: [TranslatePipe],
  template: `<span class="badge" [class]="tone()">{{ 'Web.Print' + status() | t }}</span>`,
  styles: `
    .badge {
      display: inline-flex;
      align-items: center;
      padding: 2px 10px;
      border-radius: var(--r-pill);
      font-size: var(--fs-caption);
      font-weight: 600;
      background: var(--surface-alt);
      color: var(--text-2);
    }
    .success {
      background: var(--success-subtle);
      color: var(--success);
    }
    .danger {
      background: var(--danger-subtle);
      color: var(--danger);
    }
    .warning {
      background: var(--warning-subtle);
      color: var(--warning);
    }
    .info {
      background: var(--info-subtle);
      color: var(--info);
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<PrintJobStatus>();
  protected readonly tone = computed(() => TONE[this.status()] ?? 'muted');
}

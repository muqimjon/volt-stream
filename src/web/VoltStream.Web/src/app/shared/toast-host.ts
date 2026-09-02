import { Component, inject } from '@angular/core';
import { ToastService } from '../core/ui/toast';

@Component({
  selector: 'app-toast-host',
  template: `
    <div class="host" role="status" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <button type="button" class="toast" [class]="toast.kind" (click)="toasts.dismiss(toast.id)">
          {{ toast.text }}
        </button>
      }
    </div>
  `,
  styles: `
    .host {
      position: fixed;
      left: 50%;
      transform: translateX(-50%);
      bottom: calc(var(--tabbar-h) + var(--safe-b) + var(--sp-md));
      z-index: 60;
      display: flex;
      flex-direction: column;
      gap: var(--sp-sm);
      width: min(520px, calc(100vw - 24px));
      pointer-events: none;
    }
    .toast {
      pointer-events: auto;
      text-align: start;
      border: 1px solid var(--border);
      border-radius: var(--r-md);
      padding: var(--sp-md);
      background: var(--surface);
      color: var(--text);
      box-shadow: var(--shadow-popup);
      white-space: pre-line;
    }
    .toast.success {
      background: var(--success-subtle);
      border-color: var(--success);
      color: var(--success);
    }
    .toast.danger {
      background: var(--danger-subtle);
      border-color: var(--danger);
      color: var(--danger);
    }
    .toast.warning {
      background: var(--warning-subtle);
      border-color: var(--warning);
      color: var(--warning);
    }
  `,
})
export class ToastHost {
  protected readonly toasts = inject(ToastService);
}

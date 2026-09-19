import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { TranslatePipe } from '../core/i18n/translate';
import { DialogService } from '../core/ui/dialog';

@Component({
  selector: 'app-confirm-dialog',
  imports: [TranslatePipe],
  template: `
    <dialog #dlg (cancel)="$event.preventDefault(); answer(false)">
      @if (dialog.request(); as req) {
        <h3>{{ req.title | t }}</h3>
        @if (req.choices; as choices) {
          <div class="choices">
            @for (choice of choices; track choice.id) {
              <button
                type="button"
                class="btn btn-block"
                [class.btn-primary]="choice.id === req.selectedId"
                [class.btn-ghost]="choice.id !== req.selectedId"
                (click)="answer(true, choice.id)"
              >
                {{ choice.label }}
              </button>
            }
          </div>
          <div class="actions">
            <button type="button" class="btn btn-ghost" (click)="answer(false)">
              {{ req.noKey | t }}
            </button>
          </div>
        } @else {
          <p>{{ req.message }}</p>
          <div class="actions">
            <button type="button" class="btn btn-ghost" (click)="answer(false)">
              {{ req.noKey | t }}
            </button>
            <button
              type="button"
              class="btn"
              [class.btn-primary]="!req.danger"
              [class.btn-danger]="req.danger"
              (click)="answer(true)"
            >
              {{ req.yesKey | t }}
            </button>
          </div>
        }
      }
    </dialog>
  `,
  styles: `
    dialog {
      border: 1px solid var(--border);
      border-radius: var(--r-lg);
      background: var(--surface);
      color: var(--text);
      padding: var(--sp-lg);
      width: min(420px, calc(100vw - 32px));
      box-shadow: var(--shadow-popup);
    }
    dialog::backdrop {
      background: rgba(0, 0, 0, 0.45);
    }
    h3 {
      font-size: var(--fs-subtitle);
      margin-bottom: var(--sp-sm);
    }
    p {
      white-space: pre-line;
      color: var(--text-2);
      margin-bottom: var(--sp-lg);
    }
    .actions {
      display: flex;
      justify-content: flex-end;
      gap: var(--sp-sm);
    }
    .choices {
      display: flex;
      flex-direction: column;
      gap: var(--sp-sm);
      margin-bottom: var(--sp-lg);
    }
    @media (min-width: 1024px) {
      dialog {
        box-shadow: var(--shadow-card);
      }
      .actions .btn {
        min-width: 96px;
      }
    }
  `,
})
export class ConfirmDialog {
  protected readonly dialog = inject(DialogService);
  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  constructor() {
    effect(() => {
      const el = this.dlg().nativeElement;
      if (this.dialog.request()) {
        if (!el.open) el.showModal();
      } else if (el.open) el.close();
    });
  }

  protected answer(ok: boolean, choice?: number): void {
    this.dialog.answer(ok, choice);
  }
}

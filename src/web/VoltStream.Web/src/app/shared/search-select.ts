import { Component, computed, input, model, output, signal, viewChild, ElementRef } from '@angular/core';
import { TranslatePipe } from '../core/i18n/translate';

@Component({
  selector: 'app-search-select',
  imports: [TranslatePipe],
  template: `
    <div class="field">
      @if (label()) {
        <label>{{ label() }}</label>
      }
      <button type="button" class="input trigger" [disabled]="disabled()" (click)="open()">
        <span [class.ph]="!selected()">{{ display() }}</span>
        <span class="chev">▾</span>
      </button>
      @if (hint()) {
        <small class="hint">{{ hint() }}</small>
      }
    </div>

    <dialog #dlg class="sheet" (click)="backdrop($event)">
      <div class="sheet-body">
        <div class="sheet-head">
          <input
            class="input"
            type="search"
            autocomplete="off"
            [value]="query()"
            [placeholder]="'Common.Search' | t"
            (input)="query.set($any($event.target).value)"
          />
          <button type="button" class="btn btn-ghost" (click)="close()">{{ 'Common.Close' | t }}</button>
        </div>
        <ul class="sheet-list">
          @if (clearable() && selected()) {
            <li>
              <button type="button" class="opt muted" (click)="pick(null)">{{ 'Common.Clear' | t }}</button>
            </li>
          }
          @for (item of filtered(); track $index) {
            <li>
              <button type="button" class="opt" (click)="pick(item)">
                <span>{{ labelOf()(item) }}</span>
                @if (subLabelOf()) {
                  <small>{{ subLabelOf()!(item) }}</small>
                }
              </button>
            </li>
          }
          @if (showCreate()) {
            <li>
              <button type="button" class="opt create" (click)="emitCreate()">
                {{ 'Web.CreateNew' | t: query().trim() }}
              </button>
            </li>
          }
          @if (!filtered().length && !showCreate()) {
            <li class="empty">{{ 'Web.NothingFound' | t }}</li>
          }
        </ul>
      </div>
    </dialog>
  `,
  styles: `
    .trigger {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--sp-sm);
      text-align: start;
      overflow: hidden;
    }
    .trigger > span:first-child {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .ph {
      color: var(--text-3);
    }
    .chev {
      color: var(--text-2);
    }
    .hint {
      color: var(--text-2);
      font-size: var(--fs-caption);
    }
    dialog.sheet {
      border: none;
      padding: 0;
      margin: auto auto 0;
      width: min(560px, 100%);
      max-height: 82dvh;
      background: transparent;
    }
    dialog.sheet::backdrop {
      background: rgba(0, 0, 0, 0.45);
    }
    .sheet-body {
      background: var(--surface);
      border-radius: var(--r-lg) var(--r-lg) 0 0;
      padding: var(--sp-md) var(--sp-md) calc(var(--sp-md) + var(--safe-b));
      display: flex;
      flex-direction: column;
      gap: var(--sp-sm);
      max-height: 82dvh;
    }
    .sheet-head {
      display: flex;
      gap: var(--sp-sm);
    }
    .sheet-list {
      list-style: none;
      margin: 0;
      padding: 0;
      overflow-y: auto;
    }
    .opt {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      width: 100%;
      min-height: var(--touch);
      padding: var(--sp-sm) var(--sp-md);
      border: none;
      border-radius: var(--r-md);
      background: transparent;
      text-align: start;
    }
    .opt small {
      color: var(--text-2);
      font-size: var(--fs-caption);
    }
    .opt.create {
      color: var(--brand);
      font-weight: 600;
    }
    .empty {
      padding: var(--sp-lg);
      color: var(--text-2);
      text-align: center;
    }
  `,
})
export class SearchSelect<T> {
  readonly label = input('');
  readonly placeholder = input('');
  readonly hint = input('');
  readonly disabled = input(false);
  readonly clearable = input(false);
  readonly allowCreate = input(false);
  readonly items = input<T[]>([]);
  readonly labelOf = input.required<(item: T) => string>();
  readonly subLabelOf = input<((item: T) => string) | null>(null);
  readonly selected = model<T | null>(null);

  readonly opened = output<void>();
  readonly create = output<string>();

  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');
  protected readonly query = signal('');

  protected readonly display = computed(() => {
    const item = this.selected();
    return item ? this.labelOf()(item) : this.placeholder();
  });

  protected readonly filtered = computed(() => {
    const q = this.query().trim().toLowerCase();
    const list = this.items();
    if (!q) return list;
    return list.filter((i) => this.labelOf()(i).toLowerCase().includes(q));
  });

  protected readonly showCreate = computed(() => {
    const q = this.query().trim();
    if (!this.allowCreate() || !q) return false;
    return !this.items().some((i) => this.labelOf()(i).toLowerCase() === q.toLowerCase());
  });

  open(): void {
    if (this.disabled()) return;
    this.query.set('');
    this.opened.emit();
    this.dlg().nativeElement.showModal();
  }

  close(): void {
    this.dlg().nativeElement.close();
  }

  protected pick(item: T | null): void {
    this.selected.set(item);
    this.close();
  }

  protected emitCreate(): void {
    const value = this.query().trim();
    this.close();
    this.create.emit(value);
  }

  protected backdrop(event: MouseEvent): void {
    if (event.target === this.dlg().nativeElement) this.close();
  }
}

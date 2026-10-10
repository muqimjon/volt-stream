import { Component, ElementRef, computed, effect, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '../../core/i18n/translate';
import { PermissionService } from '../../core/permissions/permission';
import { PaletteService } from '../../core/ui/palette';
import { NAV_ITEMS, NavItem } from './nav';
import { Icon } from '../../shared/icon';

@Component({
  selector: 'app-command-palette',
  imports: [TranslatePipe, Icon],
  template: `
    <dialog #dlg (cancel)="$event.preventDefault(); close()" (click)="backdrop($event)">
      <div class="search">
        <app-icon name="search" [size]="18" />
        <input
          #field
          type="text"
          role="combobox"
          aria-controls="palette-list"
          aria-expanded="true"
          autocomplete="off"
          [placeholder]="'Web.PalettePlaceholder' | t"
          [value]="query()"
          (input)="onQuery($any($event.target).value)"
          (keydown)="onKey($event)"
        />
      </div>
      <ul id="palette-list" role="listbox">
        @for (item of results(); track item.path; let i = $index) {
          <li
            role="option"
            [attr.aria-selected]="i === index()"
            [class.active]="i === index()"
            (mouseenter)="index.set(i)"
            (click)="go(item)"
          >
            <app-icon [name]="item.icon" [size]="18" />
            <span>{{ item.key | t }}</span>
            <app-icon class="go" name="arrowRight" [size]="14" />
          </li>
        } @empty {
          <li class="empty">{{ 'Web.NothingFound' | t }}</li>
        }
      </ul>
      <p class="hints">{{ 'Web.PaletteHints' | t }}</p>
    </dialog>
  `,
  styles: `
    dialog {
      width: min(560px, calc(100vw - 32px));
      margin: 90px auto 0;
      padding: 0;
      border: 1px solid var(--border);
      border-radius: 14px;
      background: var(--surface);
      color: var(--text);
      overflow: hidden;
      box-shadow: var(--shadow-popup);
    }
    dialog::backdrop {
      background: rgba(0, 0, 0, 0.4);
    }
    .search {
      display: flex;
      align-items: center;
      gap: var(--sp-md);
      padding: 14px var(--sp-lg) 10px;
      border-bottom: 1px solid var(--border);
      color: var(--text-2);
    }
    input {
      flex: 1;
      min-width: 0;
      border: none;
      outline: none;
      background: transparent;
      color: var(--text);
      font-size: 16px;
    }
    input::placeholder {
      color: var(--text-3);
    }
    ul {
      list-style: none;
      margin: 0;
      padding: var(--sp-sm);
      max-height: 320px;
      overflow-y: auto;
    }
    li {
      display: flex;
      align-items: center;
      gap: var(--sp-md);
      padding: 10px var(--sp-md);
      border-radius: var(--r-md);
      cursor: pointer;
    }
    li app-icon {
      color: var(--brand);
    }
    li.active {
      background: var(--brand-subtle);
    }
    li .go {
      margin-inline-start: auto;
      color: var(--text-3);
    }
    li.empty {
      justify-content: center;
      color: var(--text-2);
      cursor: default;
    }
    .hints {
      margin: 0;
      padding: var(--sp-sm) var(--sp-lg);
      background: var(--surface-alt);
      color: var(--text-3);
      font-size: var(--fs-caption);
    }
  `,
})
export class CommandPalette {
  private readonly palette = inject(PaletteService);
  private readonly permissions = inject(PermissionService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');
  private readonly field = viewChild.required<ElementRef<HTMLInputElement>>('field');

  protected readonly query = signal('');
  protected readonly index = signal(0);

  private readonly items = computed(() =>
    NAV_ITEMS.filter((i) => !i.permission || this.permissions.has(i.permission)),
  );
  protected readonly results = computed(() => {
    const q = this.query().trim().toLowerCase();
    return this.items().filter((i) => !q || this.translate.t(i.key).toLowerCase().includes(q));
  });

  constructor() {
    effect(() => {
      const el = this.dlg().nativeElement;
      if (this.palette.open()) {
        if (el.open) return;
        this.query.set('');
        this.index.set(0);
        el.showModal();
        this.field().nativeElement.focus();
      } else if (el.open) el.close();
    });
  }

  protected onQuery(value: string): void {
    this.query.set(value);
    this.index.set(0);
  }

  protected onKey(event: KeyboardEvent): void {
    const count = this.results().length;
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      const step = event.key === 'ArrowDown' ? 1 : -1;
      if (count) this.index.set((this.index() + step + count) % count);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const item = this.results()[this.index()];
      if (item) this.go(item);
    }
  }

  protected go(item: NavItem): void {
    this.close();
    void this.router.navigateByUrl(item.path);
  }

  protected close(): void {
    this.palette.open.set(false);
  }

  protected backdrop(event: MouseEvent): void {
    if (event.target === this.dlg().nativeElement) this.close();
  }
}

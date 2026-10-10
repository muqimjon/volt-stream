import {
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  signal,
  untracked,
} from '@angular/core';
import { TranslatePipe } from '../core/i18n/translate';
import { Category, Product, WarehouseItem } from '../core/models';
import { closeOnBack } from '../core/ui/back-stack';
import { fmtNum } from '../core/utils/number';
import { Icon } from './icon';
import { NumberField } from './number-field';
import { StockFlow } from './stock-flow';
import { StockSelection } from './stock-selection';

@Component({
  selector: 'app-product-sheet',
  imports: [TranslatePipe, Icon, NumberField, StockFlow],
  template: `
    @if (open()) {
      <div class="sheet" [style.height.px]="viewport().height" [style.top.px]="viewport().top">
        <header class="bar">
          <button type="button" class="link" (click)="back()">
            <app-icon name="chevronLeft" [size]="20" />
            {{ 'Web.Back' | t }}
          </button>
          @if (added()) {
            <span class="muted">{{ 'Web.AddedCount' | t: added() }}</span>
          }
          <button type="button" class="link done" (click)="close()">{{ 'Web.Done' | t }}</button>
        </header>

        @if (flash()) {
          <div class="flash">{{ 'Web.Added' | t }}</div>
        }

        @switch (step()) {
          @case (1) {
            <div class="search">
              <app-icon name="search" [size]="18" />
              <input
                type="search"
                enterkeyhint="search"
                autocomplete="off"
                [placeholder]="'Common.Search' | t"
                [value]="query()"
                (input)="query.set($any($event.target).value)"
              />
            </div>
            <div class="chips">
              <button type="button" [class.on]="!chip()" (click)="setChip(null)">
                {{ 'Common.All' | t }}
              </button>
              @for (category of sel.shownCategories(); track category.id) {
                <button
                  type="button"
                  [class.on]="chip()?.id === category.id"
                  (click)="setChip(category)"
                >
                  {{ category.name }}
                </button>
              }
            </div>
            <ul class="rows">
              @for (product of list(); track product.id) {
                <li>
                  <button type="button" (click)="pick(product)">
                    <strong>{{ product.name }}</strong>
                    <small>{{ sel.productHint(product) }}</small>
                  </button>
                </li>
              } @empty {
                <li class="empty">{{ 'Web.NothingFound' | t }}</li>
              }
            </ul>
          }
          @case (2) {
            <div class="who">
              <strong>{{ sel.product()?.name }}</strong>
              <small>{{ 'Sales.RollLength' | t }}</small>
            </div>
            <ul class="rows">
              @for (stock of lengths(); track stock.id) {
                <li>
                  <button type="button" (click)="choose(stock)">
                    <strong>{{ sel.stockLabel(stock) }}</strong>
                    <small>{{ sel.stockHint(stock) }}</small>
                  </button>
                </li>
              } @empty {
                <li class="empty">{{ 'Web.NothingFound' | t }}</li>
              }
            </ul>
          }
          @default {
            <div class="entry" appStockFlow (rowSubmit)="add()">
              <div class="who">
                <strong>{{ sel.product()?.name }}</strong>
                @if (sel.stock(); as stock) {
                  <small>{{ sel.stockLabel(stock) }} · {{ sel.stockHint(stock) }}</small>
                }
              </div>
              <div class="stepper">
                <button type="button" class="btn btn-ghost" (click)="sel.step(-1)">−</button>
                <app-number-field
                  data-step="count"
                  [label]="'Sales.RollCount' | t"
                  [value]="sel.rollCount()"
                  (valueChange)="sel.rollCount.set($event)"
                  (commit)="sel.onRollCountCommit($event)"
                />
                <button type="button" class="btn btn-ghost" (click)="sel.step(1)">+</button>
              </div>
              <app-number-field
                [label]="'Web.TotalLength' | t"
                [value]="sel.length()"
                (valueChange)="sel.length.set($event)"
                (commit)="sel.onLengthCommit($event)"
              />
              @if (priced()) {
                <div class="grid-2">
                  <app-number-field
                    [label]="'Common.Price' | t"
                    [value]="unitPrice()"
                    (valueChange)="unitPrice.set($event)"
                  />
                  <app-number-field
                    [label]="'Sales.DiscountPercent' | t"
                    [value]="discountRate()"
                    (valueChange)="discountRate.set($event)"
                  />
                </div>
              }
              @if (priced() && total() !== null) {
                <div class="total">
                  <span>{{ 'Sales.GrandTotal' | t }}</span>
                  <strong>{{ fmt(total()) }}</strong>
                </div>
              }
            </div>
            <footer>
              <button type="button" class="btn btn-primary btn-block" (click)="add()">
                {{ (editing() ? 'Common.Save' : 'Common.Add') | t }}
              </button>
            </footer>
          }
        }
      </div>
    }
  `,
  styles: `
    .sheet {
      position: fixed;
      inset-inline: 0;
      z-index: 58;
      display: flex;
      flex-direction: column;
      background: var(--canvas);
      animation: sheet-up 0.2s ease;
    }
    @keyframes sheet-up {
      from {
        transform: translateY(100%);
      }
    }
    .bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--sp-sm);
      flex: 0 0 auto;
      min-height: 52px;
      padding: var(--safe-t) var(--sp-sm) 0;
      background: var(--surface);
      border-bottom: 1px solid var(--border);
    }
    .link {
      display: inline-flex;
      align-items: center;
      gap: 2px;
      min-height: var(--touch);
      padding: 0 var(--sp-sm);
      border: none;
      background: transparent;
      color: var(--brand);
      font-weight: 600;
    }
    .flash {
      flex: 0 0 auto;
      padding: var(--sp-sm) var(--sp-lg);
      background: var(--success-subtle);
      color: var(--success);
      font-weight: 600;
      text-align: center;
    }
    .search {
      display: flex;
      align-items: center;
      gap: var(--sp-sm);
      flex: 0 0 auto;
      margin: var(--sp-md) var(--sp-md) 0;
      padding: 0 var(--sp-md);
      border: 1px solid var(--border-strong);
      border-radius: var(--r-md);
      background: var(--surface);
      color: var(--text-2);
    }
    .search input {
      flex: 1;
      min-width: 0;
      min-height: var(--touch);
      border: none;
      outline: none;
      background: transparent;
      color: var(--text);
    }
    .chips {
      display: flex;
      gap: var(--sp-sm);
      flex: 0 0 auto;
      padding: var(--sp-md);
      overflow-x: auto;
      scrollbar-width: none;
    }
    .chips button {
      flex: 0 0 auto;
      min-height: 36px;
      padding: 0 var(--sp-md);
      border: 1px solid var(--border);
      border-radius: var(--r-pill);
      background: var(--surface);
      color: var(--text-2);
      font-weight: 600;
    }
    .chips button.on {
      border-color: var(--brand);
      background: var(--brand-subtle);
      color: var(--brand);
    }
    .rows {
      flex: 1;
      min-height: 0;
      margin: 0;
      padding: 0 var(--sp-md) var(--sp-md);
      list-style: none;
      overflow-y: auto;
    }
    .rows button {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 2px;
      width: 100%;
      min-height: 56px;
      margin-bottom: var(--sp-sm);
      padding: var(--sp-sm) var(--sp-lg);
      border: 1px solid var(--border);
      border-radius: var(--r-md);
      background: var(--surface);
      color: var(--text);
      text-align: start;
    }
    .rows small,
    .who small {
      color: var(--text-2);
      font-size: var(--fs-caption);
    }
    .empty {
      padding: var(--sp-xl);
      color: var(--text-2);
      text-align: center;
    }
    .who {
      display: flex;
      flex-direction: column;
      gap: 2px;
      flex: 0 0 auto;
      padding: var(--sp-md) var(--sp-lg) var(--sp-sm);
    }
    .entry {
      flex: 1;
      min-height: 0;
      display: flex;
      flex-direction: column;
      gap: var(--sp-lg);
      padding-bottom: var(--sp-lg);
      overflow-y: auto;
    }
    .entry > app-number-field,
    .entry > .grid-2,
    .stepper {
      margin-inline: var(--sp-lg);
    }
    .stepper {
      display: flex;
      align-items: flex-end;
      gap: var(--sp-sm);
    }
    .stepper app-number-field {
      flex: 1;
      min-width: 0;
    }
    .stepper .btn {
      min-width: 64px;
      min-height: 56px;
      padding: 0;
      font-size: 28px;
    }
    .total {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      margin-inline: var(--sp-lg);
      padding: var(--sp-md) var(--sp-lg);
      border-radius: var(--r-md);
      background: var(--brand-subtle);
      color: var(--brand);
    }
    .total strong {
      font-size: var(--fs-title);
    }
    footer {
      flex: 0 0 auto;
      padding: var(--sp-md) var(--sp-lg) calc(var(--sp-md) + var(--safe-b));
      background: var(--surface);
      border-top: 1px solid var(--border);
    }
    footer .btn {
      min-height: 56px;
      font-size: var(--fs-subtitle);
    }
  `,
})
export class ProductSheet {
  readonly open = model(false);
  readonly start = input<1 | 3>(1);
  readonly priced = input(false);
  readonly unitPrice = model<number | null>(null);
  readonly discountRate = model<number | null>(null);
  readonly total = input<number | null>(null);
  readonly adder = input.required<() => Promise<boolean>>();

  protected readonly sel = inject(StockSelection);
  protected readonly fmt = fmtNum;
  protected readonly steps = signal<number[]>([1]);
  protected readonly step = computed(() => this.steps().at(-1) ?? 1);
  protected readonly query = signal('');
  protected readonly chip = signal<Category | null>(null);
  protected readonly flash = signal(false);
  protected readonly added = signal(0);
  protected readonly editing = signal(false);
  protected readonly viewport = signal({ height: window.innerHeight, top: 0 });

  protected readonly list = computed(() => {
    const q = this.query().trim().toLowerCase();
    const chip = this.chip();
    return this.sel
      .shownProducts()
      .filter(
        (p) => (!chip || p.categoryId === chip.id) && (!q || p.name.toLowerCase().includes(q)),
      );
  });

  protected readonly lengths = computed(() =>
    [...this.sel.stocks()].sort(
      (a, b) =>
        Number(a.rollCount <= 0) - Number(b.rollCount <= 0) ||
        b.rollCount - a.rollCount ||
        b.lengthPerRoll - a.lengthPerRoll,
    ),
  );

  constructor() {
    effect(() => {
      if (this.open()) untracked(() => this.begin());
    });

    effect((onCleanup) => {
      const vv = window.visualViewport;
      if (!this.open() || !vv) return;
      const update = () => this.viewport.set({ height: vv.height, top: vv.offsetTop });
      update();
      vv.addEventListener('resize', update);
      vv.addEventListener('scroll', update);
      onCleanup(() => {
        vv.removeEventListener('resize', update);
        vv.removeEventListener('scroll', update);
      });
    });

    closeOnBack(
      () => this.open(),
      () => this.back(),
    );
  }

  protected back(): boolean {
    const steps = this.steps();
    if (steps.length > 1) {
      this.steps.set(steps.slice(0, -1));
      return true;
    }
    this.open.set(false);
    return false;
  }

  protected close(): void {
    this.open.set(false);
  }

  protected setChip(category: Category | null): void {
    this.chip.set(category);
    this.sel.category.set(category);
  }

  protected async pick(product: Product): Promise<void> {
    await this.sel.onProduct(product, false);
    const options = this.sel.stocks();
    if (options.length === 1) this.choose(options[0]);
    else this.go(2);
  }

  protected choose(stock: WarehouseItem): void {
    this.sel.onStock(stock, false);
    this.sel.rollCount.set(1);
    void this.sel.onRollCountCommit(1);
    this.go(3);
  }

  protected async add(): Promise<void> {
    if (!(await this.adder()())) return;
    if (this.editing()) return this.close();
    this.added.update((n) => n + 1);
    this.flash.set(true);
    setTimeout(() => this.flash.set(false), 1500);
    this.restart();
  }

  private begin(): void {
    const start = this.start();
    this.editing.set(start === 3);
    this.added.set(0);
    this.steps.set([start]);
    this.query.set('');
    this.chip.set(null);
    if (start === 1) {
      this.sel.clear();
      void this.sel.loadProducts();
    }
  }

  private go(step: number): void {
    this.steps.update((steps) => [...steps, step]);
  }

  private restart(): void {
    this.steps.set([1]);
    this.query.set('');
    this.chip.set(null);
    void this.sel.loadProducts();
  }
}

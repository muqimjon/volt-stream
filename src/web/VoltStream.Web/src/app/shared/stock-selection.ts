import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { ApiClient } from '../core/api/api-client';
import { AuthService } from '../core/auth/auth';
import { TranslateService } from '../core/i18n/translate';
import { Category, GeneralSettings, Product, StockSummary, WarehouseItem } from '../core/models';
import { DialogService } from '../core/ui/dialog';
import { ToastService } from '../core/ui/toast';
import { fmtNum, mod, round } from '../core/utils/number';

@Injectable()
export class StockSelection {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(DialogService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  readonly categories = signal<Category[]>([]);
  readonly products = signal<Product[]>([]);
  readonly stocks = signal<WarehouseItem[]>([]);

  readonly category = signal<Category | null>(null);
  readonly product = signal<Product | null>(null);
  readonly stock = signal<WarehouseItem | null>(null);
  readonly rollCount = signal<number | null>(null);
  readonly length = signal<number | null>(null);

  readonly focusStep = signal<{ target: 'length' | 'count'; n: number } | null>(null);

  private readonly allowNegativeStock = signal(true);
  private readonly summary = signal<StockSummary[]>([]);
  private readonly catalog = signal<Product[]>([]);
  private readonly totals = computed(() => new Map(this.summary().map((s) => [s.productId, s])));
  readonly shownCategories = computed(() => {
    if (this.allowNegativeStock()) return this.categories();
    const ids = new Set(
      this.catalog()
        .filter((p) => this.totals().has(p.id))
        .map((p) => p.categoryId),
    );
    return this.categories().filter((c) => ids.has(c.id));
  });
  readonly shownProducts = computed(() => {
    if (this.allowNegativeStock()) return this.products();
    return this.products().filter((p) => this.totals().has(p.id));
  });

  private readonly showBranch = computed(
    () => this.auth.isAdmin() && this.auth.branchId() === null,
  );
  readonly stockLabel = (item: WarehouseItem) => fmtNum(item.lengthPerRoll);
  readonly stockHint = (item: WarehouseItem) => {
    const info = this.translate.t(
      'Web.StockInfo',
      fmtNum(item.rollCount, 0),
      fmtNum(item.totalLength),
    );
    return this.showBranch() ? `${item.warehouseName} · ${info}` : info;
  };

  readonly productHint = (item: Product) => {
    const sum = this.totals().get(item.id);
    const info = this.translate.t(
      'Web.StockInfo',
      fmtNum(sum?.rollCount ?? 0, 0),
      fmtNum(sum?.totalLength ?? 0),
    );
    return this.category() || !item.category ? info : `${item.category.name} · ${info}`;
  };

  private pendingCommit: Promise<void> = Promise.resolve();
  private readonly productStocks = new Map<number, Promise<WarehouseItem[]>>();
  private stockLoad: Promise<void> | null = null;
  private stockBranch: number | null | undefined;

  constructor() {
    void this.loadCategories();
    void this.loadSettings();
    effect(() => {
      this.auth.branchId();
      if (!this.allowNegativeStock())
        untracked(() => {
          void this.ensureStock();
          void this.loadCatalog();
        });
    });
  }

  settled(): Promise<void> {
    return this.pendingCommit;
  }

  clear(): void {
    this.productStocks.clear();
    this.category.set(null);
    this.product.set(null);
    this.stock.set(null);
    this.products.set([]);
    this.stocks.set([]);
    this.rollCount.set(null);
    this.length.set(null);
  }

  async loadProducts(): Promise<void> {
    const filters: Record<string, string[]> = { category: ['include'] };
    const category = this.category();
    if (category) filters['CategoryId'] = [`=${category.id}`];
    void this.ensureStock();
    const page = await this.api
      .filter<Product>('products/filter', { filters, page: 1, pageSize: 500, descending: false })
      .catch(() => ({ items: [], paging: null }));
    this.products.set(page.items);
  }

  onCategory(category: Category | null): void {
    this.category.set(category);
    this.product.set(null);
    this.stock.set(null);
    this.stocks.set([]);
    this.products.set([]);
  }

  async onProduct(product: Product | null, chain = true): Promise<void> {
    this.product.set(product);
    if (product?.category) this.category.set(product.category);
    this.stock.set(null);
    this.stocks.set([]);
    if (!product) return;

    const branchId = this.auth.branchId();
    const items = await this.api
      .get<WarehouseItem[]>(
        `warehouse-items/productId=${product.id}`,
        branchId === null ? undefined : { branchId },
      )
      .catch(() => []);
    const options = items
      .filter((s) => this.allowNegativeStock() || s.rollCount > 0)
      .sort(
        (a, b) =>
          a.lengthPerRoll - b.lengthPerRoll || a.warehouseName.localeCompare(b.warehouseName),
      );
    this.stocks.set(options);
    if (!chain) return;
    if (options.length === 1) this.onStock(options[0]);
    else if (options.length) this.requestStep('length');
  }

  onStock(stock: WarehouseItem | null): void {
    this.stock.set(stock);
    if (!stock) return;
    this.syncLengthFromRolls();
    this.requestStep('count');
  }

  onRollCountCommit(value: number | null): Promise<void> {
    return (this.pendingCommit = this.commitRollCount(value));
  }

  onLengthCommit(value: number | null): Promise<void> {
    return (this.pendingCommit = this.commitLength(value));
  }

  async edit(line: {
    productId: number;
    product: string;
    category: string;
    categoryId: number;
    unit: string;
    lengthPerRoll: number;
    rollCount: number;
    totalLength: number;
  }): Promise<void> {
    this.category.set({ id: line.categoryId, name: line.category });
    await this.onProduct(
      {
        id: line.productId,
        name: line.product,
        unit: line.unit,
        categoryId: line.categoryId,
        isWeightTracked: false,
      },
      false,
    );
    this.stock.set(
      this.stocks().find((s) => s.lengthPerRoll === line.lengthPerRoll) ?? {
        id: 0,
        lengthPerRoll: line.lengthPerRoll,
        rollCount: 0,
        totalLength: 0,
        unitPrice: 0,
        discountRate: 0,
        warehouseName: '',
      },
    );
    this.rollCount.set(line.rollCount);
    this.length.set(line.totalLength);
  }

  async adjust(
    line: {
      productId: number;
      product: string;
      lengthPerRoll: number;
      rollCount: number;
      totalLength: number;
    },
    delta: number,
  ): Promise<{ rollCount: number; totalLength: number } | null> {
    const rollCount = line.rollCount + delta;
    const totalLength = round(line.totalLength + delta * line.lengthPerRoll, 3);
    if (rollCount <= 0 || totalLength <= 0) return null;
    if (delta > 0) {
      const stock = await this.stockOf(line.productId, line.lengthPerRoll);
      if (stock && rollCount > stock.rollCount) {
        const message = this.translate.t('Sales.RollStockWarning', line.product, stock.rollCount);
        if (!(await this.confirmOversell(stock, message, line.product))) return null;
      }
    }
    return { rollCount, totalLength };
  }

  step(delta: number): Promise<void> {
    const next = (this.rollCount() ?? 0) + delta;
    if (next <= 0) {
      this.rollCount.set(null);
      this.length.set(null);
      return Promise.resolve();
    }
    this.rollCount.set(next);
    return this.onRollCountCommit(next);
  }

  private async loadCategories(): Promise<void> {
    this.categories.set(await this.api.get<Category[]>('categories').catch(() => []));
  }

  private async loadSettings(): Promise<void> {
    const settings = await this.api
      .get<GeneralSettings>('settings/general', undefined, true)
      .catch(() => null);
    this.allowNegativeStock.set(settings?.allowNegativeStock ?? true);
  }

  private ensureStock(): Promise<void> {
    const branchId = this.auth.branchId();
    if (this.stockLoad && this.stockBranch === branchId) return this.stockLoad;
    this.stockBranch = branchId;
    return (this.stockLoad = this.api
      .get<StockSummary[]>('warehouse-items/summary', branchId === null ? undefined : { branchId })
      .then((rows) => this.summary.set(rows))
      .catch(() => undefined));
  }

  private async loadCatalog(): Promise<void> {
    if (this.catalog().length) return;
    const page = await this.api
      .filter<Product>('products/filter', { page: 1, pageSize: 500, descending: false })
      .catch(() => ({ items: [], paging: null }));
    this.catalog.set(page.items);
  }

  private async stockOf(
    productId: number,
    lengthPerRoll: number,
  ): Promise<WarehouseItem | undefined> {
    let rows = this.productStocks.get(productId);
    if (!rows) {
      const branchId = this.auth.branchId();
      rows = this.api
        .get<WarehouseItem[]>(
          `warehouse-items/productId=${productId}`,
          branchId === null ? undefined : { branchId },
        )
        .catch(() => []);
      this.productStocks.set(productId, rows);
    }
    return (await rows).find((s) => s.lengthPerRoll === lengthPerRoll);
  }

  private requestStep(target: 'length' | 'count'): void {
    this.focusStep.set({ target, n: (this.focusStep()?.n ?? 0) + 1 });
  }

  private async confirmOversell(
    stock: WarehouseItem,
    message: string,
    name = this.product()?.name ?? '',
  ): Promise<boolean> {
    if (this.allowNegativeStock())
      return this.dialog.confirm(message, { title: 'Sales.SaleCaption' });
    const available = this.translate.t(
      'Web.StockInfo',
      fmtNum(stock.rollCount, 0),
      fmtNum(stock.totalLength),
    );
    this.toast.error(this.translate.t('Web.StockNotEnough', name, available));
    return false;
  }

  private async commitRollCount(value: number | null): Promise<void> {
    const stock = this.stock();
    if (value !== null && stock && value > stock.rollCount) {
      const ok = await this.confirmOversell(
        stock,
        this.translate.t('Sales.RollStockWarning', this.product()?.name ?? '', stock.rollCount),
      );
      if (!ok) {
        this.rollCount.set(null);
        return;
      }
    }
    this.syncLengthFromRolls();
  }

  private async commitLength(value: number | null): Promise<void> {
    const stock = this.stock();
    const perRoll = stock?.lengthPerRoll ?? 0;
    if (value === null || value <= 0 || perRoll <= 0) {
      this.length.set(null);
      return;
    }

    if (value > stock!.totalLength) {
      const ok = await this.confirmOversell(
        stock!,
        this.translate.t(
          'Sales.MeterStockWarning',
          this.product()?.name ?? '',
          fmtNum(perRoll),
          fmtNum(stock!.totalLength),
        ),
      );
      if (!ok) {
        this.length.set(null);
        return;
      }
    }

    if (mod(value, perRoll) !== 0) {
      const wholeRolls = Math.trunc(value / perRoll);
      const wholeLength = round(wholeRolls * perRoll, 3);
      const cut = round(value - wholeLength, 3);
      const residue = round(perRoll - cut, 3);
      const ok = await this.dialog.confirm(
        this.translate.t(
          'Sales.RollCutWarning',
          this.product()?.name ?? '',
          fmtNum(perRoll),
          fmtNum(value),
          fmtNum(cut),
          fmtNum(residue),
        ),
        { title: 'Sales.SaleCaption' },
      );
      if (!ok) {
        this.length.set(wholeLength);
        this.rollCount.set(wholeRolls);
        return;
      }
    }

    this.rollCount.set(Math.ceil(value / perRoll));
  }

  private syncLengthFromRolls(): void {
    const rolls = this.rollCount();
    const perRoll = this.stock()?.lengthPerRoll ?? null;
    this.length.set(rolls !== null && perRoll !== null ? round(rolls * perRoll, 3) : null);
  }
}

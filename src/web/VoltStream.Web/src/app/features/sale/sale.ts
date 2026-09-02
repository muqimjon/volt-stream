import { Component, computed, inject, signal } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { TranslatePipe, TranslateService } from '../../core/i18n/translate';
import {
  Category,
  Currency,
  Customer,
  Product,
  SaleRequest,
  WarehouseItem,
} from '../../core/models';
import { PermissionService } from '../../core/permissions/permission';
import { PwaService } from '../../core/pwa/pwa';
import { PrintHubService } from '../../core/realtime/print-hub';
import { PrintService } from '../../core/realtime/print';
import { DialogService } from '../../core/ui/dialog';
import { ToastService } from '../../core/ui/toast';
import { fromDateInput, operationMoment, toIsoOffset } from '../../core/utils/date';
import { fmtNum, mod, round } from '../../core/utils/number';
import { DateField } from '../../shared/date-field';
import { EmptyState } from '../../shared/empty-state';
import { Icon } from '../../shared/icon';
import { NumberField } from '../../shared/number-field';
import { PageHeader } from '../../shared/page-header';
import { SearchSelect } from '../../shared/search-select';
import { StatusBadge } from '../../shared/status-badge';
import { DraftItem, SaleDraftStore } from './sale-draft';

@Component({
  selector: 'app-sale',
  imports: [
    TranslatePipe,
    DateField,
    NumberField,
    SearchSelect,
    PageHeader,
    EmptyState,
    StatusBadge,
    Icon,
  ],
  templateUrl: './sale.html',
  styleUrl: './sale.css',
})
export class SalePage {
  private readonly api = inject(ApiClient);
  private readonly dialog = inject(DialogService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);
  private readonly permissions = inject(PermissionService);
  private readonly hub = inject(PrintHubService);

  protected readonly translate = inject(TranslateService);
  protected readonly print = inject(PrintService);
  protected readonly pwa = inject(PwaService);
  protected readonly store = inject(SaleDraftStore);
  protected readonly fmt = fmtNum;

  protected readonly canViewPrices = computed(() => this.permissions.has('sales.view_prices'));
  protected readonly canPrint = computed(
    () => this.permissions.has('print.request') && this.auth.printPolicy() !== 'Deny',
  );

  protected readonly categories = signal<Category[]>([]);
  protected readonly products = signal<Product[]>([]);
  protected readonly stocks = signal<WarehouseItem[]>([]);
  protected readonly customers = signal<Customer[]>([]);
  private readonly currency = signal<Currency | null>(null);

  protected readonly category = signal<Category | null>(null);
  protected readonly product = signal<Product | null>(null);
  protected readonly stock = signal<WarehouseItem | null>(null);
  protected readonly customer = signal<Customer | null>(null);
  protected readonly rollCount = signal<number | null>(null);
  protected readonly length = signal<number | null>(null);
  protected readonly unitPrice = signal<number | null>(null);
  protected readonly discountRate = signal<number | null>(null);

  protected readonly submitting = signal(false);
  protected readonly savedSaleId = signal<number | null>(null);

  protected readonly totalAmount = computed(() =>
    round((this.unitPrice() ?? 0) * (this.length() ?? 0)),
  );
  protected readonly discountAmount = computed(() =>
    round(this.totalAmount() * ((this.discountRate() ?? 0) / 100)),
  );
  protected readonly finalAmount = computed(() => round(this.totalAmount() - this.discountAmount()));
  protected readonly weight = computed(() => {
    const p = this.product();
    return p?.isWeightTracked ? round((this.length() ?? 0) * (p.weightPerUnit ?? 0), 3) : 0;
  });
  protected readonly hasWeight = computed(() => this.store.draft().items.some((i) => i.weight > 0));
  protected readonly printJob = computed(() => {
    const id = this.savedSaleId();
    return id === null ? null : this.hub.jobFor(id);
  });

  protected readonly nameOf = (item: { name: string }) => item.name;
  protected readonly stockLabel = (item: WarehouseItem) => fmtNum(item.lengthPerRoll);
  protected readonly stockHint = (item: WarehouseItem) =>
    this.translate.t('Web.StockInfo', fmtNum(item.rollCount, 0), fmtNum(item.totalLength));
  protected readonly customerHint = (item: Customer) => item.phone ?? '';

  constructor() {
    const draft = this.store.draft();
    if (draft.customerId) this.customer.set({ id: draft.customerId, name: draft.customerName });
    void this.loadCategories();
    void this.loadCurrency();
  }

  private async loadCategories(): Promise<void> {
    this.categories.set(await this.api.get<Category[]>('categories').catch(() => []));
  }

  private async loadCurrency(): Promise<void> {
    const list = await this.api.get<Currency[]>('currencies').catch(() => []);
    this.currency.set(list.find((c) => c.isDefault) ?? list[0] ?? null);
  }

  protected async loadCustomers(): Promise<void> {
    if (this.customers().length) return;
    this.customers.set(await this.api.get<Customer[]>('customers').catch(() => []));
  }

  protected pickCustomer(customer: Customer | null): void {
    this.customer.set(customer);
    this.store.patch({ customerId: customer?.id ?? null, customerName: customer?.name ?? '' });
  }

  protected async createCustomer(name: string): Promise<void> {
    if (!name) return;
    try {
      const id = await this.api.post<number>('customers', {
        name,
        phone: '',
        address: '',
        description: '',
        accounts: [{ openingBalance: 0, balance: 0 }],
      });
      this.customers.set([]);
      await this.loadCustomers();
      this.pickCustomer(this.customers().find((c) => c.id === id) ?? { id, name });
    } catch {
      this.toast.error(this.translate.t('Sales.CustomerCreateError'));
    }
  }

  protected async loadProducts(): Promise<void> {
    const filters: Record<string, string[]> = { category: ['include'] };
    const category = this.category();
    if (category) filters['CategoryId'] = [`=${category.id}`];
    const page = await this.api
      .filter<Product>('products/filter', { filters, page: 1, pageSize: 500, descending: false })
      .catch(() => ({ items: [], paging: null }));
    this.products.set(page.items);
  }

  protected onCategory(category: Category | null): void {
    this.category.set(category);
    this.product.set(null);
    this.stock.set(null);
    this.stocks.set([]);
    this.products.set([]);
  }

  protected async onProduct(product: Product | null): Promise<void> {
    this.product.set(product);
    if (product?.category) this.category.set(product.category);
    this.stock.set(null);
    this.stocks.set([]);
    if (!product) return;

    const branchId = this.auth.branchId();
    this.stocks.set(
      await this.api
        .get<WarehouseItem[]>(
          `warehouse-items/productId=${product.id}`,
          branchId === null ? undefined : { branchId },
        )
        .catch(() => []),
    );
  }

  protected onStock(stock: WarehouseItem | null): void {
    this.stock.set(stock);
    if (!stock) return;
    this.unitPrice.set(stock.unitPrice);
    this.discountRate.set(stock.discountRate);
    this.syncLengthFromRolls();
  }

  protected async onRollCountCommit(value: number | null): Promise<void> {
    const stock = this.stock();
    if (value !== null && stock && value > stock.rollCount) {
      const ok = await this.dialog.confirm(
        this.translate.t('Sales.RollStockWarning', this.product()?.name ?? '', stock.rollCount),
        { title: 'Sales.SaleCaption' },
      );
      if (!ok) {
        this.rollCount.set(null);
        return;
      }
    }
    this.syncLengthFromRolls();
  }

  protected async onLengthCommit(value: number | null): Promise<void> {
    const stock = this.stock();
    const perRoll = stock?.lengthPerRoll ?? 0;
    if (value === null || value <= 0 || perRoll <= 0) {
      this.length.set(null);
      return;
    }

    if (value > stock!.totalLength) {
      const ok = await this.dialog.confirm(
        this.translate.t(
          'Sales.MeterStockWarning',
          this.product()?.name ?? '',
          fmtNum(perRoll),
          fmtNum(stock!.totalLength),
        ),
        { title: 'Sales.SaleCaption' },
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

  protected addItem(): void {
    const category = this.category();
    const product = this.product();
    const stock = this.stock();
    const rollCount = this.rollCount();
    const length = this.length();
    const price = this.unitPrice();
    const discountRate = this.discountRate() ?? 0;

    if (!category) return this.warn('Sales.CategoryNotSelected');
    if (!product) return this.warn('Sales.ProductNotSelected');
    if (!stock) return this.warn('Sales.PerRollNotSelected');
    if (rollCount === null || rollCount <= 0) return this.warn('Sales.RollCountRequired');
    if (length === null || length <= 0) return this.warn('Sales.TotalQuantityRequired');

    if (this.canViewPrices()) {
      if (price === null || price <= 0) return this.warn('Sales.PriceRequired');
      if (this.totalAmount() <= 0) return this.warn('Sales.TotalSumRequired');
      if (this.finalAmount() <= 0) return this.warn('Sales.GrandTotalRequired');
      if (discountRate < 0 || discountRate > 100) return this.warn('Sales.DiscountPercentRange');
    }

    const item: DraftItem = {
      categoryId: category.id,
      categoryName: category.name,
      productId: product.id,
      productName: product.name,
      lengthPerRoll: stock.lengthPerRoll,
      rollCount,
      totalLength: length,
      unitPrice: this.canViewPrices() ? (price ?? 0) : 0,
      discountRate: this.canViewPrices() ? discountRate : 0,
      discountAmount: this.canViewPrices() ? this.discountAmount() : 0,
      totalAmount: this.canViewPrices() ? this.totalAmount() : 0,
      finalAmount: this.canViewPrices() ? this.finalAmount() : 0,
      weight: this.weight(),
    };

    this.store.add(item);
    this.clearRow();
  }

  protected removeItem(index: number): void {
    this.store.removeAt(index);
  }

  protected async clearAll(): Promise<void> {
    if (!(await this.dialog.confirm(this.translate.t('Supplies.DeleteConfirm')))) return;
    this.store.reset();
    this.clearRow();
  }

  protected async submit(): Promise<void> {
    if (this.submitting()) return;

    const draft = this.store.draft();
    const day = fromDateInput(draft.date);
    if (!day) return this.warn('Sales.DateNotSelected');
    if (!draft.items.length) return this.warn('Sales.NoProductsAdded');

    const date = operationMoment(day);
    const distinct = new Set(draft.items.map((i) => i.productId)).size;
    const ok = await this.dialog.confirm(
      this.translate.t('Sales.SubmitConfirm', draft.date, distinct),
    );
    if (!ok) return;

    const totals = this.store.totals();
    const request: SaleRequest = {
      date: toIsoOffset(date),
      customerId: draft.customerId,
      currencyId: this.currency()?.id ?? 0,
      rollCount: totals.rollCount,
      length: round(totals.length, 3),
      amount: round(totals.amount),
      isDiscountApplied: false,
      discount: round(totals.discount),
      description: draft.description || null,
      branchId: this.auth.branchId(),
      items: draft.items.map((i) => ({
        productId: i.productId,
        rollCount: i.rollCount,
        lengthPerRoll: i.lengthPerRoll,
        totalLength: i.totalLength,
        unitPrice: i.unitPrice,
        discountRate: i.discountRate,
        discountAmount: i.discountAmount,
        totalAmount: i.totalAmount,
        finalAmount: i.finalAmount,
      })),
    };

    this.submitting.set(true);
    try {
      const saleId = await this.api.post<number>('sales', request);
      this.savedSaleId.set(saleId);
      this.toast.success(this.translate.t('Sales.SaveSuccess'));
      this.store.reset();
      this.clearRow();
    } catch {
      /* reported by the error interceptor */
    } finally {
      this.submitting.set(false);
    }
  }

  protected async requestPrint(): Promise<void> {
    const id = this.savedSaleId();
    if (id !== null) await this.print.request(id);
  }

  protected closeSuccess(): void {
    this.savedSaleId.set(null);
  }

  private clearRow(): void {
    this.category.set(null);
    this.product.set(null);
    this.stock.set(null);
    this.products.set([]);
    this.stocks.set([]);
    this.rollCount.set(null);
    this.length.set(null);
    this.unitPrice.set(null);
    this.discountRate.set(null);
  }

  private warn(key: string): void {
    this.toast.warn(this.translate.t(key));
  }
}

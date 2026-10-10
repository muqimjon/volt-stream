import { Component, computed, inject, signal } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { TranslatePipe, TranslateService } from '../../core/i18n/translate';
import { Currency, Customer, SaleRequest, WarehouseItem } from '../../core/models';
import { PermissionService } from '../../core/permissions/permission';
import { PwaService } from '../../core/pwa/pwa';
import { PrintHubService } from '../../core/realtime/print-hub';
import { PrintService } from '../../core/realtime/print';
import { DialogService } from '../../core/ui/dialog';
import { ToastService } from '../../core/ui/toast';
import { fromDateInput, operationMoment, toIsoOffset } from '../../core/utils/date';
import { fmtNum, round } from '../../core/utils/number';
import { DateField } from '../../shared/date-field';
import { EmptyState } from '../../shared/empty-state';
import { Icon } from '../../shared/icon';
import { NumberField } from '../../shared/number-field';
import { PageHeader } from '../../shared/page-header';
import { SearchSelect } from '../../shared/search-select';
import { StatusBadge } from '../../shared/status-badge';
import { StockSelection } from '../../shared/stock-selection';
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
  providers: [StockSelection],
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
  protected readonly sel = inject(StockSelection);
  protected readonly fmt = fmtNum;

  protected readonly canViewPrices = computed(() => this.permissions.has('sales.view_prices'));
  protected readonly canPrint = computed(
    () => this.permissions.has('print.request') && this.auth.printPolicy() !== 'Deny',
  );

  protected readonly customers = signal<Customer[]>([]);
  private readonly currency = signal<Currency | null>(null);

  protected readonly customer = signal<Customer | null>(null);
  protected readonly unitPrice = signal<number | null>(null);
  protected readonly discountRate = signal<number | null>(null);

  protected readonly submitting = signal(false);
  protected readonly savedSaleId = signal<number | null>(null);
  private savedBranchId: number | null = null;

  protected readonly totalAmount = computed(() =>
    round((this.unitPrice() ?? 0) * (this.sel.length() ?? 0)),
  );
  protected readonly discountAmount = computed(() =>
    round(this.totalAmount() * ((this.discountRate() ?? 0) / 100)),
  );
  protected readonly finalAmount = computed(() =>
    round(this.totalAmount() - this.discountAmount()),
  );
  protected readonly weight = computed(() => {
    const p = this.sel.product();
    return p?.isWeightTracked ? round((this.sel.length() ?? 0) * (p.weightPerUnit ?? 0), 3) : 0;
  });
  protected readonly hasWeight = computed(() => this.store.draft().items.some((i) => i.weight > 0));
  protected readonly printJob = computed(() => {
    const id = this.savedSaleId();
    return id === null ? null : this.hub.jobFor(id);
  });

  protected readonly nameOf = (item: { name: string }) => item.name;
  protected readonly customerHint = (item: Customer) => item.phone ?? '';

  constructor() {
    const draft = this.store.draft();
    if (draft.customerId) this.customer.set({ id: draft.customerId, name: draft.customerName });
    void this.loadCurrency();
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

  protected onStock(stock: WarehouseItem | null): void {
    this.sel.onStock(stock);
    if (!stock) return;
    this.unitPrice.set(stock.unitPrice);
    this.discountRate.set(stock.discountRate);
  }

  protected async addItem(): Promise<void> {
    await this.sel.settled();
    const category = this.sel.category();
    const product = this.sel.product();
    const stock = this.sel.stock();
    const rollCount = this.sel.rollCount();
    const length = this.sel.length();
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

    if (this.auth.isAdmin() && this.auth.branchId() === null)
      return this.warn('Header.SelectBranch');

    if (!this.customer()) return this.warn('Sales.CustomerRequired');

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
      description: draft.description || '',
      branchId: this.auth.branchId(),
      items: [...draft.items].reverse().map((i) => ({
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
      this.savedBranchId = this.auth.branchId();
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
    if (id !== null) await this.print.request(id, this.savedBranchId);
  }

  protected closeSuccess(): void {
    this.savedSaleId.set(null);
  }

  private clearRow(): void {
    this.sel.clear();
    this.unitPrice.set(null);
    this.discountRate.set(null);
  }

  private warn(key: string): void {
    this.toast.warn(this.translate.t(key));
  }
}

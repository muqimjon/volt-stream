import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { TranslatePipe, TranslateService } from '../../core/i18n/translate';
import { Currency, Customer, SaleRequest } from '../../core/models';
import { PermissionService } from '../../core/permissions/permission';
import { PwaService } from '../../core/pwa/pwa';
import { BreakpointService } from '../../core/ui/breakpoint';
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
import { ProductSheet } from '../../shared/product-sheet';
import { SearchSelect } from '../../shared/search-select';
import { StatusBadge } from '../../shared/status-badge';
import { StockFlow } from '../../shared/stock-flow';
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
    StockFlow,
    ProductSheet,
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
  protected readonly bp = inject(BreakpointService);
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

  protected readonly sheetOpen = signal(false);
  protected readonly sheetStart = signal<1 | 3>(1);
  protected readonly noteOpen = signal(false);
  protected readonly adder = () => this.addItem();
  private editedAt: number | null = null;
  private replacing = false;
  private editedWeightPerUnit: number | null = null;
  private keepPrice = false;

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

  protected readonly balance = computed(() => {
    const accounts = this.customer()?.accounts;
    if (!this.canViewPrices() || !accounts) return null;
    const sum = accounts.reduce((total, a) => total + a.balance, 0);
    const current = Math.sign(sum) * Math.round(Math.abs(sum));
    return {
      current,
      after: current - this.store.totals().final * (this.currency()?.exchangeRate || 1),
    };
  });

  protected readonly nameOf = (item: { name: string }) => item.name;
  protected readonly customerHint = (item: Customer) => item.phone ?? '';

  constructor() {
    const draft = this.store.draft();
    if (draft.customerId) {
      this.customer.set({ id: draft.customerId, name: draft.customerName });
      if (this.canViewPrices()) void this.restoreCustomer(draft.customerId);
    }
    void this.loadCurrency();
    effect(() => {
      const stock = this.sel.stock();
      if (!stock) return;
      untracked(() => {
        if (this.keepPrice) return void (this.keepPrice = false);
        this.unitPrice.set(stock.unitPrice);
        this.discountRate.set(stock.discountRate);
      });
    });
    effect(() => {
      if (!this.sheetOpen()) untracked(() => this.endEdit());
    });
  }

  private endEdit(): void {
    this.editedAt = null;
    this.replacing = false;
    this.editedWeightPerUnit = null;
    this.keepPrice = false;
    this.sel.clear();
    this.unitPrice.set(null);
    this.discountRate.set(null);
  }

  protected openSheet(): void {
    this.sheetStart.set(1);
    this.sheetOpen.set(true);
  }

  protected async editItem(index: number): Promise<void> {
    const item = this.store.draft().items[index];
    this.editedAt = index;
    this.replacing = true;
    this.editedWeightPerUnit = item.totalLength ? item.weight / item.totalLength : 0;
    this.keepPrice = true;
    await this.sel.edit({
      productId: item.productId,
      product: item.productName,
      category: item.categoryName,
      categoryId: item.categoryId,
      unit: '',
      lengthPerRoll: item.lengthPerRoll,
      rollCount: item.rollCount,
      totalLength: item.totalLength,
    });
    this.unitPrice.set(item.unitPrice);
    this.discountRate.set(item.discountRate);
    this.sheetStart.set(3);
    this.sheetOpen.set(true);
  }

  protected async adjustItem(index: number, delta: number): Promise<void> {
    const item = this.store.draft().items[index];
    const next = await this.sel.adjust(
      {
        productId: item.productId,
        product: item.productName,
        lengthPerRoll: item.lengthPerRoll,
        rollCount: item.rollCount,
        totalLength: item.totalLength,
      },
      delta,
    );
    if (!next) return;
    const totalAmount = round(item.unitPrice * next.totalLength);
    const discountAmount = round(totalAmount * (item.discountRate / 100));
    this.store.replaceAt(index, {
      ...item,
      rollCount: next.rollCount,
      totalLength: next.totalLength,
      totalAmount,
      discountAmount,
      finalAmount: round(totalAmount - discountAmount),
      weight: item.totalLength ? round((item.weight / item.totalLength) * next.totalLength, 3) : 0,
    });
  }

  private async restoreCustomer(id: number): Promise<void> {
    await this.loadCustomers();
    const customer = this.customers().find((c) => c.id === id);
    if (customer && this.customer()?.id === id) this.customer.set(customer);
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

  protected async addItem(): Promise<boolean> {
    await this.sel.settled();
    const fail = (key: string) => {
      this.warn(key);
      return false;
    };
    const category = this.sel.category();
    const product = this.sel.product();
    const stock = this.sel.stock();
    const rollCount = this.sel.rollCount();
    const length = this.sel.length();
    const price = this.unitPrice();
    const discountRate = this.discountRate() ?? 0;

    if (!category) return fail('Sales.CategoryNotSelected');
    if (!product) return fail('Sales.ProductNotSelected');
    if (!stock) return fail('Sales.PerRollNotSelected');
    if (rollCount === null || rollCount <= 0) return fail('Sales.RollCountRequired');
    if (length === null || length <= 0) return fail('Sales.TotalQuantityRequired');

    if (this.canViewPrices()) {
      if (price === null || price <= 0) return fail('Sales.PriceRequired');
      if (this.totalAmount() <= 0) return fail('Sales.TotalSumRequired');
      if (this.finalAmount() <= 0) return fail('Sales.GrandTotalRequired');
      if (discountRate < 0 || discountRate > 100) return fail('Sales.DiscountPercentRange');
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
      weight:
        this.editedWeightPerUnit === null
          ? this.weight()
          : round(length * this.editedWeightPerUnit, 3),
    };

    if (this.replacing && this.editedAt !== null) this.store.replaceAt(this.editedAt, item);
    else this.store.add(item);
    this.editedAt = null;
    this.replacing = false;
    this.editedWeightPerUnit = null;
    this.clearRow();
    return true;
  }

  protected removeItem(index: number): void {
    this.editedAt = null;
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

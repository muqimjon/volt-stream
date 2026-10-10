import { Component, computed, inject, signal } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { TranslatePipe, TranslateService } from '../../core/i18n/translate';
import { Category, Product, SupplyListItem, SupplyRequest, WarehouseItem } from '../../core/models';
import { PermissionService } from '../../core/permissions/permission';
import { PwaService } from '../../core/pwa/pwa';
import { DialogService } from '../../core/ui/dialog';
import { ToastService } from '../../core/ui/toast';
import { fromDateInput, operationMoment, toDateInput, toIsoOffset } from '../../core/utils/date';
import { fmtNum, round } from '../../core/utils/number';
import { DateField } from '../../shared/date-field';
import { EmptyState } from '../../shared/empty-state';
import { NumberField } from '../../shared/number-field';
import { PageHeader } from '../../shared/page-header';
import { SearchSelect } from '../../shared/search-select';

@Component({
  selector: 'app-supply',
  imports: [TranslatePipe, DateField, NumberField, SearchSelect, PageHeader, EmptyState],
  templateUrl: './supply.html',
  styleUrl: './supply.css',
})
export class SupplyPage {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(DialogService);
  private readonly toast = inject(ToastService);
  private readonly permissions = inject(PermissionService);

  protected readonly translate = inject(TranslateService);
  protected readonly pwa = inject(PwaService);
  protected readonly fmt = fmtNum;

  protected readonly canViewPrices = computed(() => this.permissions.has('sales.view_prices'));

  protected readonly date = signal(toDateInput(new Date()));
  protected readonly categories = signal<Category[]>([]);
  protected readonly products = signal<Product[]>([]);
  protected readonly stocks = signal<WarehouseItem[]>([]);
  protected readonly supplies = signal<SupplyListItem[]>([]);

  protected readonly category = signal<Category | null>(null);
  protected readonly product = signal<Product | null>(null);
  protected readonly stock = signal<WarehouseItem | null>(null);
  protected readonly unit = signal('');
  protected readonly rollCount = signal<number | null>(null);
  protected readonly unitPrice = signal<number | null>(null);
  protected readonly discountRate = signal<number | null>(null);
  protected readonly saving = signal(false);

  protected readonly totalLength = computed(() =>
    round((this.rollCount() ?? 0) * (this.stock()?.lengthPerRoll ?? 0), 3),
  );

  protected readonly nameOf = (item: { name: string }) => item.name;
  protected readonly stockLabel = (item: WarehouseItem) => fmtNum(item.lengthPerRoll);

  constructor() {
    void this.loadCategories();
    void this.loadSupplies();
  }

  private async loadCategories(): Promise<void> {
    this.categories.set(await this.api.get<Category[]>('categories').catch(() => []));
  }

  protected async loadProducts(): Promise<void> {
    const filters: Record<string, string[]> = { category: ['include'] };
    const category = this.category();
    if (category && category.id > 0) filters['CategoryId'] = [`=${category.id}`];
    const page = await this.api
      .filter<Product>('products/filter', { filters, page: 1, pageSize: 500, descending: false })
      .catch(() => ({ items: [], paging: null }));
    this.products.set(page.items);
  }

  protected async loadSupplies(): Promise<void> {
    const page = await this.api
      .filter<SupplyListItem>(
        'supplies/filter',
        {
          filters: { date: [this.date()], product: ['include:category'] },
          page: 1,
          pageSize: 50,
          descending: true,
        },
        { branchScoped: true },
      )
      .catch(() => ({ items: [], paging: null }));
    this.supplies.set(page.items);
  }

  protected onDate(value: string): void {
    this.date.set(value);
    void this.loadSupplies();
  }

  protected onCategory(category: Category | null): void {
    this.category.set(category);
    this.product.set(null);
    this.products.set([]);
    this.stock.set(null);
    this.stocks.set([]);
  }

  protected async createCategory(name: string): Promise<void> {
    if (!name) return;
    const ok = await this.dialog.confirm(
      `'${name}' ${this.translate.t('Supplies.AddAsNewCategory')}`,
      { title: 'Supplies.ConfirmTitle' },
    );
    if (!ok) return;
    this.onCategory({ id: 0, name });
  }

  protected async onProduct(product: Product | null): Promise<void> {
    this.product.set(product);
    this.stock.set(null);
    this.stocks.set([]);
    if (!product) return;

    this.unit.set(product.unit || 'metr');
    if (product.category) this.category.set(product.category);
    if (product.id <= 0) return;

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

  protected async createProduct(name: string): Promise<void> {
    if (!name) return;
    const ok = await this.dialog.confirm(
      `'${name}' ${this.translate.t('Supplies.AddAsNewProduct')}`,
      { title: 'Supplies.ConfirmTitle' },
    );
    if (!ok) return;
    await this.onProduct({
      id: 0,
      name,
      unit: this.unit() || 'metr',
      categoryId: this.category()?.id ?? 0,
      isWeightTracked: false,
      weightPerUnit: null,
    });
  }

  protected onStock(stock: WarehouseItem | null): void {
    this.stock.set(stock);
    if (!stock || stock.id <= 0) return;
    this.unitPrice.set(stock.unitPrice);
    this.discountRate.set(stock.discountRate);
  }

  protected async createStock(text: string): Promise<void> {
    const value = Number(text.replace(',', '.'));
    if (!Number.isFinite(value) || value <= 0) return;
    const ok = await this.dialog.confirm(
      `'${text}' ${this.translate.t('Supplies.AddAsNewBundleSize')}`,
      { title: 'Supplies.ConfirmTitle' },
    );
    if (!ok) return;
    this.stock.set({
      id: 0,
      lengthPerRoll: value,
      rollCount: 0,
      totalLength: 0,
      unitPrice: 0,
      discountRate: 0,
      warehouseName: '',
    });
    this.unitPrice.set(null);
    this.discountRate.set(null);
  }

  protected onDiscount(value: number | null): void {
    if (value !== null && value > 100) {
      this.toast.warn(this.translate.t('Supplies.DiscountMaxWarning'));
      this.discountRate.set(100);
    } else this.discountRate.set(value);
  }

  protected async save(): Promise<void> {
    if (this.saving()) return;

    const category = this.category();
    const product = this.product();
    const stock = this.stock();
    const rollCount = this.rollCount();

    if (this.auth.isAdmin() && this.auth.branchId() === null)
      return this.toast.warn(this.translate.t('Header.SelectBranch'));

    if (!category?.name || !product?.name)
      return this.toast.error(this.translate.t('Supplies.CategoryProductRequired'));
    if (!this.unit().trim()) return this.toast.warn(this.translate.t('Supplies.UnitRequired'));
    if (
      !rollCount ||
      rollCount <= 0 ||
      !stock ||
      stock.lengthPerRoll <= 0 ||
      (this.canViewPrices() && !(this.unitPrice() && this.unitPrice()! > 0))
    )
      return this.toast.warn(this.translate.t('Supplies.QuantityFieldsRequired'));

    const day = fromDateInput(this.date());
    if (!day) return this.toast.warn(this.translate.t('Sales.DateNotSelected'));

    const request: SupplyRequest = {
      date: toIsoOffset(operationMoment(day)),
      categoryId: category.id,
      categoryName: category.name,
      productId: product.id,
      productName: product.name,
      rollCount,
      unit: this.unit().trim(),
      lengthPerRoll: stock.lengthPerRoll,
      totalLength: this.totalLength(),
      unitPrice: this.canViewPrices() ? (this.unitPrice() ?? null) : null,
      discountRate: this.canViewPrices() ? (this.discountRate() ?? null) : null,
      branchId: this.auth.branchId(),
    };

    this.saving.set(true);
    try {
      await this.api.post<number>('supplies', request);
      this.toast.success(this.translate.t('Settings.SavedSuccess'));
      this.clearForm();
      await this.loadSupplies();
    } catch {
      /* reported by the error interceptor */
    } finally {
      this.saving.set(false);
    }
  }

  protected clearForm(): void {
    this.category.set(null);
    this.product.set(null);
    this.products.set([]);
    this.stock.set(null);
    this.stocks.set([]);
    this.unit.set('');
    this.rollCount.set(null);
    this.unitPrice.set(null);
    this.discountRate.set(null);
  }
}

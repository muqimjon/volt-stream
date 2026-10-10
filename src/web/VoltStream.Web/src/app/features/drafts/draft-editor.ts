import {
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client';
import { AuthService } from '../../core/auth/auth';
import { TranslatePipe, TranslateService } from '../../core/i18n/translate';
import { Customer, SaleDraft, SaleDraftRequest } from '../../core/models';
import { PermissionService } from '../../core/permissions/permission';
import { PwaService } from '../../core/pwa/pwa';
import { PrintService } from '../../core/realtime/print';
import { PrintHubService } from '../../core/realtime/print-hub';
import { BreakpointService } from '../../core/ui/breakpoint';
import { DialogService } from '../../core/ui/dialog';
import { ToastService } from '../../core/ui/toast';
import { fromDateInput, operationMoment, toDateInput, toIsoOffset } from '../../core/utils/date';
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

interface EditorItem {
  productId: number;
  categoryId: number;
  product: string;
  category: string;
  unit: string;
  lengthPerRoll: number;
  rollCount: number;
  totalLength: number;
}

@Component({
  selector: 'app-draft-editor',
  imports: [
    TranslatePipe,
    RouterLink,
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
  templateUrl: './draft-editor.html',
  styleUrls: ['../sale/sale.css', './draft-editor.css'],
})
export class DraftEditorPage {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(DialogService);
  private readonly hub = inject(PrintHubService);
  private readonly permissions = inject(PermissionService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  protected readonly sel = inject(StockSelection);
  protected readonly print = inject(PrintService);
  protected readonly pwa = inject(PwaService);
  protected readonly bp = inject(BreakpointService);
  protected readonly fmt = fmtNum;

  protected readonly id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id')) || null;
  private draftBranchId: number | null = null;

  protected readonly date = signal(toDateInput(new Date()));
  protected readonly customer = signal<Customer | null>(null);
  protected readonly customers = signal<Customer[]>([]);
  protected readonly description = signal('');
  protected readonly items = signal<EditorItem[]>([]);
  protected readonly saving = signal(false);
  protected readonly savedId = signal<number | null>(null);
  private readonly picker = viewChild<ElementRef<HTMLElement>>('picker');
  protected readonly sheetOpen = signal(false);
  protected readonly sheetStart = signal<1 | 3>(1);
  protected readonly noteOpen = signal(false);
  protected readonly adder = () => this.addItem();
  private editedAt: number | null = null;
  private replacing = false;

  protected readonly totals = computed(() => ({
    rolls: this.items().reduce((sum, i) => sum + i.rollCount, 0),
    length: round(
      this.items().reduce((sum, i) => sum + i.totalLength, 0),
      3,
    ),
  }));
  protected readonly canPrint = computed(
    () => this.permissions.has('drafts.print') && this.auth.printPolicy() !== 'Deny',
  );
  protected readonly printJob = computed(() => {
    const id = this.savedId() ?? this.id;
    return id === null ? null : this.hub.jobFor(id, 'draft');
  });

  protected readonly nameOf = (item: { name: string }) => item.name;
  protected readonly customerHint = (item: Customer) => item.phone ?? '';

  constructor() {
    if (this.id !== null) void this.loadDraft(this.id);
    effect(() => {
      if (!this.sheetOpen()) untracked(() => this.endEdit());
    });
  }

  private endEdit(): void {
    this.editedAt = null;
    this.replacing = false;
    this.sel.clear();
  }

  private async loadDraft(id: number): Promise<void> {
    try {
      const draft = await this.api.get<SaleDraft>(`sale-drafts/${id}`);
      if (draft.status !== 'Open') return void this.router.navigate(['/drafts']);
      this.draftBranchId = draft.branchId;
      this.date.set(toDateInput(new Date(draft.date)));
      if (draft.customerId) this.customer.set({ id: draft.customerId, name: draft.customer ?? '' });
      this.description.set(draft.description);
      this.items.set(draft.items);
    } catch {
      void this.router.navigate(['/drafts']);
    }
  }

  protected async loadCustomers(): Promise<void> {
    if (this.customers().length) return;
    this.customers.set(await this.api.get<Customer[]>('customers').catch(() => []));
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

    if (!category) return fail('Sales.CategoryNotSelected');
    if (!product) return fail('Sales.ProductNotSelected');
    if (!stock) return fail('Sales.PerRollNotSelected');
    if (rollCount === null || rollCount <= 0) return fail('Sales.RollCountRequired');
    if (length === null || length <= 0) return fail('Sales.TotalQuantityRequired');

    this.items.update((list) => {
      const next = [...list];
      next.splice(this.editedAt ?? next.length, this.replacing ? 1 : 0, {
        productId: product.id,
        categoryId: category.id,
        product: product.name,
        category: category.name,
        unit: product.unit,
        lengthPerRoll: stock.lengthPerRoll,
        rollCount,
        totalLength: length,
      });
      return next;
    });
    this.editedAt = null;
    this.replacing = false;
    this.sel.clear();
    return true;
  }

  protected openSheet(): void {
    this.sheetStart.set(1);
    this.sheetOpen.set(true);
  }

  protected async editInSheet(index: number): Promise<void> {
    this.editedAt = index;
    this.replacing = true;
    await this.sel.edit(this.items()[index]);
    this.sheetStart.set(3);
    this.sheetOpen.set(true);
  }

  protected async adjustItem(index: number, delta: number): Promise<void> {
    const next = await this.sel.adjust(this.items()[index], delta);
    if (next)
      this.items.update((list) =>
        list.map((item, i) => (i === index ? { ...item, ...next } : item)),
      );
  }

  protected async editItem(index: number): Promise<void> {
    const item = this.items()[index];
    this.removeItem(index);
    this.editedAt = index;
    await this.sel.edit(item);
    this.picker()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  protected removeItem(index: number): void {
    this.editedAt = null;
    this.items.update((list) => list.filter((_, i) => i !== index));
  }

  protected async save(): Promise<void> {
    if (this.saving()) return;
    if (this.id === null && this.auth.isAdmin() && this.auth.branchId() === null)
      return this.warn('Header.SelectBranch');

    const day = fromDateInput(this.date());
    if (!day) return this.warn('Sales.DateNotSelected');
    if (!this.items().length) return this.warn('Sales.NoProductsAdded');

    const request: SaleDraftRequest = {
      date: toIsoOffset(operationMoment(day)),
      customerId: this.customer()?.id ?? null,
      description: this.description(),
      items: this.items().map((i) => ({
        productId: i.productId,
        lengthPerRoll: i.lengthPerRoll,
        rollCount: i.rollCount,
        totalLength: i.totalLength,
      })),
    };

    this.saving.set(true);
    try {
      if (this.id === null) {
        const branchId = this.auth.branchId();
        this.savedId.set(await this.api.post<number>('sale-drafts', { ...request, branchId }));
        this.draftBranchId = branchId;
      } else {
        await this.api.put<boolean>('sale-drafts', { ...request, id: this.id });
        this.savedId.set(this.id);
      }
    } catch {
      /* reported by the error interceptor */
    } finally {
      this.saving.set(false);
    }
  }

  protected async cancel(): Promise<void> {
    const ok = await this.dialog.confirm(this.translate.t('Web.DraftCancelConfirm'), {
      danger: true,
    });
    if (!ok) return;
    try {
      await this.api.post<boolean>(`sale-drafts/${this.id}/cancel`, {});
      this.toast.success(this.translate.t('Web.DraftCancelled'));
      await this.router.navigate(['/drafts']);
    } catch {
      /* reported by the error interceptor */
    }
  }

  protected requestPrint(): void {
    const id = this.savedId() ?? this.id;
    if (id !== null) void this.print.request(id, this.draftBranchId, 'draft');
  }

  protected startNew(): void {
    if (this.id !== null) return void this.router.navigateByUrl('/drafts/new');
    this.savedId.set(null);
    this.date.set(toDateInput(new Date()));
    this.customer.set(null);
    this.description.set('');
    this.items.set([]);
    this.sel.clear();
  }

  private warn(key: string): void {
    this.toast.warn(this.translate.t(key));
  }
}

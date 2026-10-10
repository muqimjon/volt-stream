import { Injectable, computed, effect, signal } from '@angular/core';
import { toDateInput } from '../../core/utils/date';

const KEY = 'vs.sale-draft';

export interface DraftItem {
  categoryId: number;
  categoryName: string;
  productId: number;
  productName: string;
  lengthPerRoll: number;
  rollCount: number;
  totalLength: number;
  unitPrice: number;
  discountRate: number;
  discountAmount: number;
  totalAmount: number;
  finalAmount: number;
  weight: number;
}

export interface SaleDraft {
  date: string;
  customerId: number | null;
  customerName: string;
  description: string;
  items: DraftItem[];
}

const empty = (): SaleDraft => ({
  date: toDateInput(new Date()),
  customerId: null,
  customerName: '',
  description: '',
  items: [],
});

@Injectable({ providedIn: 'root' })
export class SaleDraftStore {
  readonly draft = signal<SaleDraft>(read());

  readonly totals = computed(() => {
    const items = this.draft().items;
    return {
      rollCount: items.reduce((s, i) => s + i.rollCount, 0),
      length: items.reduce((s, i) => s + i.totalLength, 0),
      weight: items.reduce((s, i) => s + i.weight, 0),
      amount: items.reduce((s, i) => s + i.totalAmount, 0),
      discount: items.reduce((s, i) => s + i.discountAmount, 0),
      final: items.reduce((s, i) => s + i.finalAmount, 0),
    };
  });

  constructor() {
    effect(() => sessionStorage.setItem(KEY, JSON.stringify(this.draft())));
  }

  patch(value: Partial<SaleDraft>): void {
    this.draft.update((d) => ({ ...d, ...value }));
  }

  add(item: DraftItem): void {
    this.draft.update((d) => ({ ...d, items: [item, ...d.items] }));
  }

  replaceAt(index: number, item: DraftItem): void {
    this.draft.update((d) => ({ ...d, items: d.items.map((o, i) => (i === index ? item : o)) }));
  }

  removeAt(index: number): void {
    this.draft.update((d) => ({ ...d, items: d.items.filter((_, i) => i !== index) }));
  }

  reset(): void {
    this.draft.set(empty());
  }
}

function read(): SaleDraft {
  try {
    const raw = sessionStorage.getItem(KEY);
    if (!raw) return empty();
    const parsed = JSON.parse(raw) as SaleDraft;
    return { ...empty(), ...parsed, items: Array.isArray(parsed.items) ? parsed.items : [] };
  } catch {
    return empty();
  }
}

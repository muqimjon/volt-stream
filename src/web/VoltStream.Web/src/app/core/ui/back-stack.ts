import { DestroyRef, Injectable, effect, inject, untracked } from '@angular/core';
import { BreakpointService } from './breakpoint';

interface Entry {
  id: number;
  onBack: () => boolean | void;
}

// Phones only: the Android/browser Back button must close the topmost overlay instead of leaving the page.
@Injectable({ providedIn: 'root' })
export class BackStack {
  private readonly phone = inject(BreakpointService).phone;
  private readonly stack: Entry[] = [];
  private seq = 0;
  private silent = 0;

  constructor() {
    window.addEventListener('popstate', () => this.onPop());
  }

  push(onBack: () => boolean | void): number {
    if (!this.phone()) return 0;
    const id = ++this.seq;
    this.stack.push({ id, onBack });
    this.mark(id);
    return id;
  }

  release(id: number): void {
    const index = this.stack.findIndex((entry) => entry.id === id);
    if (index < 0) return;
    this.stack.splice(index, 1);
    if (history.state?.vsBack !== id) return;
    this.silent++;
    history.back();
  }

  private mark(id: number): void {
    history.pushState({ ...history.state, vsBack: id }, '');
  }

  private onPop(): void {
    if (this.silent) {
      this.silent--;
      return;
    }
    const entry = this.stack.pop();
    if (entry?.onBack() !== true) return;
    this.stack.push(entry);
    this.mark(entry.id);
  }
}

// `onBack` runs after Back popped the entry; returning true keeps the overlay open (it consumed a step).
export function closeOnBack(open: () => boolean, onBack: () => boolean | void): void {
  const stack = inject(BackStack);
  let token = 0;
  effect(() => {
    const isOpen = open();
    untracked(() => {
      if (isOpen && !token) token = stack.push(onBack);
      else if (!isOpen && token) {
        stack.release(token);
        token = 0;
      }
    });
  });
  inject(DestroyRef).onDestroy(() => stack.release(token));
}

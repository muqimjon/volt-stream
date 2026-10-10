import { DestroyRef, Injectable, effect, inject, untracked } from '@angular/core';
import { BreakpointService } from './breakpoint';

interface Entry {
  id: number;
  onBack: () => void;
}

// Phones only: the Android/browser Back button must close the topmost overlay instead of leaving the page.
// Entries are only ever pushed from user-initiated code: Chromium skips script-added entries on Back.
@Injectable({ providedIn: 'root' })
export class BackStack {
  private readonly phone = inject(BreakpointService).phone;
  private readonly stack: Entry[] = [];
  private seq = 0;
  private silent = 0;

  constructor() {
    window.addEventListener('popstate', () => this.onPop());
  }

  push(onBack: () => void): number {
    if (!this.phone()) return 0;
    const id = ++this.seq;
    this.stack.push({ id, onBack });
    history.pushState({ ...history.state, vsBack: id }, '');
    return id;
  }

  release(...ids: number[]): void {
    const top = this.stack.at(-1)?.id;
    let count = 0;
    while (count < this.stack.length && ids.includes(this.stack[this.stack.length - 1 - count].id))
      count++;
    for (const id of ids) {
      const index = this.stack.findIndex((entry) => entry.id === id);
      if (index >= 0) this.stack.splice(index, 1);
    }
    if (!count || history.state?.vsBack !== top) return;
    this.silent++;
    history.go(-count);
  }

  private onPop(): void {
    if (this.silent) {
      this.silent--;
      return;
    }
    this.stack.pop()?.onBack();
  }
}

// `onBack` runs after Back already popped the entry; closing any other way releases it.
export function closeOnBack(open: () => boolean, onBack: () => void): void {
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

import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { BreakpointService } from './breakpoint';

const TEXT_TYPES = ['text', 'search', 'number', 'tel', 'email', 'url', 'password'];

function isTextField(target: EventTarget | null): boolean {
  if (target instanceof HTMLTextAreaElement) return true;
  return target instanceof HTMLInputElement && TEXT_TYPES.includes(target.type);
}

// The Android WebView shell resizes the layout viewport for the keyboard (Chrome only shrinks the visual one),
// so fixed bars would ride up above it; hiding them on phones makes both behave the same.
@Injectable({ providedIn: 'root' })
export class KeyboardService {
  private readonly bp = inject(BreakpointService);
  private readonly focused = signal(false);
  private readonly shrunk = signal(false);

  readonly open = computed(() => this.bp.phone() && (this.focused() || this.shrunk()));

  constructor() {
    document.addEventListener('focusin', (event) => this.focused.set(isTextField(event.target)));
    document.addEventListener('focusout', (event) => {
      if (!isTextField(event.relatedTarget)) this.focused.set(false);
    });

    const vv = window.visualViewport;
    if (vv) {
      let width = vv.width;
      let base = vv.height;
      vv.addEventListener('resize', () => {
        if (vv.width !== width) {
          width = vv.width;
          base = vv.height;
        }
        base = Math.max(base, vv.height);
        this.shrunk.set(base - vv.height > 150);
      });
    }

    effect(() => document.documentElement.classList.toggle('kb-open', this.open()));
  }
}

import {
  Directive,
  ElementRef,
  Injector,
  afterNextRender,
  effect,
  inject,
  output,
} from '@angular/core';
import { StockSelection } from './stock-selection';

@Directive({
  selector: '[appStockFlow]',
  host: { '(keydown.enter)': 'onEnter($event)' },
})
export class StockFlow {
  readonly rowSubmit = output<void>();

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  constructor() {
    const sel = inject(StockSelection);
    const injector = inject(Injector);
    effect(() => {
      const step = sel.focusStep();
      if (!step) return;
      afterNextRender(
        () => {
          if (step.target === 'length')
            this.host.querySelector<HTMLElement>('[data-step=length] .trigger')?.click();
          else this.host.querySelector<HTMLElement>('[data-step=count] input')?.focus();
        },
        { injector },
      );
    });
  }

  protected onEnter(event: Event): void {
    const input = event.target as HTMLElement;
    if (!(input instanceof HTMLInputElement) || !input.closest('app-number-field')) return;
    event.preventDefault();
    input.blur();
    this.rowSubmit.emit();
  }
}

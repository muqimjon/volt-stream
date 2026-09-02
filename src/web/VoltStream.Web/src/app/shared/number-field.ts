import { Component, effect, input, model, output, signal } from '@angular/core';
import { parseDecimal } from '../core/utils/number';
import { DecimalInputDirective } from './decimal-input';

@Component({
  selector: 'app-number-field',
  imports: [DecimalInputDirective],
  template: `
    <div class="field">
      @if (label()) {
        <label [attr.for]="id">{{ label() }}</label>
      }
      <input
        appDecimalInput
        class="input"
        type="text"
        inputmode="decimal"
        autocomplete="off"
        [id]="id"
        [value]="text()"
        [disabled]="disabled()"
        [placeholder]="placeholder()"
        (focus)="focused.set(true)"
        (input)="onInput($event)"
        (blur)="onBlur()"
      />
    </div>
  `,
})
export class NumberField {
  readonly label = input('');
  readonly placeholder = input('');
  readonly disabled = input(false);
  readonly value = model<number | null>(null);
  readonly commit = output<number | null>();

  readonly id = `nf-${Math.random().toString(36).slice(2, 8)}`;
  protected readonly text = signal('');
  protected readonly focused = signal(false);

  constructor() {
    effect(() => {
      const v = this.value();
      if (this.focused()) return;
      this.text.set(v === null || v === undefined ? '' : String(v));
    });
  }

  protected onInput(event: Event): void {
    const raw = (event.target as HTMLInputElement).value;
    this.text.set(raw);
    this.value.set(parseDecimal(raw));
  }

  protected onBlur(): void {
    this.focused.set(false);
    const parsed = parseDecimal(this.text());
    this.value.set(parsed);
    this.text.set(parsed === null ? '' : String(parsed));
    this.commit.emit(parsed);
  }
}

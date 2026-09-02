import { Component, input, model } from '@angular/core';
import { toDateInput } from '../core/utils/date';

@Component({
  selector: 'app-date-field',
  template: `
    <div class="field">
      @if (label()) {
        <label [attr.for]="id">{{ label() }}</label>
      }
      <input
        class="input"
        type="date"
        [id]="id"
        [value]="value()"
        [max]="max()"
        [disabled]="disabled()"
        (change)="value.set($any($event.target).value)"
      />
    </div>
  `,
})
export class DateField {
  readonly label = input('');
  readonly disabled = input(false);
  readonly max = input(toDateInput(new Date()));
  readonly value = model<string>(toDateInput(new Date()));
  readonly id = `df-${Math.random().toString(36).slice(2, 8)}`;
}

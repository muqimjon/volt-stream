import { Component, input, model } from '@angular/core';
import { TranslatePipe } from '../core/i18n/translate';
import { addDays, fromDateInput, toDateInput } from '../core/utils/date';
import { Icon } from './icon';

@Component({
  selector: 'app-date-field',
  imports: [TranslatePipe, Icon],
  template: `
    <div class="field">
      @if (label()) {
        <label [attr.for]="id">{{ label() }}</label>
      }
      <div class="control">
        @if (stepper()) {
          <button
            type="button"
            class="btn btn-ghost step"
            [title]="'Web.PrevDay' | t"
            [disabled]="disabled()"
            (click)="shift(-1)"
          >
            <app-icon name="chevronLeft" [size]="18" />
          </button>
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
        @if (stepper()) {
          <button
            type="button"
            class="btn btn-ghost step"
            [title]="'Web.NextDay' | t"
            [disabled]="disabled() || value() >= max()"
            (click)="shift(1)"
          >
            <app-icon name="chevronRight" [size]="18" />
          </button>
          <button
            type="button"
            class="btn btn-ghost today"
            [disabled]="disabled()"
            (click)="value.set(max())"
          >
            {{ 'Web.Today' | t }}
          </button>
        }
      </div>
    </div>
  `,
  styles: `
    .control {
      display: flex;
      gap: var(--sp-xs);
    }
    input {
      flex: 1;
      min-width: 0;
    }
    .step {
      padding: 0;
      min-width: var(--touch);
    }
    .today {
      white-space: nowrap;
    }
  `,
})
export class DateField {
  readonly label = input('');
  readonly disabled = input(false);
  readonly stepper = input(false);
  readonly max = input(toDateInput(new Date()));
  readonly value = model<string>(toDateInput(new Date()));
  readonly id = `df-${Math.random().toString(36).slice(2, 8)}`;

  protected shift(days: number): void {
    const day = fromDateInput(this.value());
    if (!day) return;
    const next = toDateInput(addDays(day, days));
    this.value.set(next > this.max() ? this.max() : next);
  }
}

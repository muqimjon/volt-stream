import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="empty">
      <span class="glyph">{{ glyph() }}</span>
      <p>{{ text() }}</p>
    </div>
  `,
  styles: `
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--sp-sm);
      padding: var(--sp-xxl) var(--sp-lg);
      color: var(--text-2);
      text-align: center;
    }
    .glyph {
      font-size: 32px;
      opacity: 0.6;
    }
  `,
})
export class EmptyState {
  readonly text = input('');
  readonly glyph = input('∅');
}

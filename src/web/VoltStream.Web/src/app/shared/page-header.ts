import { Component, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  template: `
    <header class="head">
      <div>
        <h1>{{ title() }}</h1>
        @if (subtitle()) {
          <p class="muted">{{ subtitle() }}</p>
        }
      </div>
      <ng-content />
    </header>
  `,
  styles: `
    .head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--sp-md);
    }
    h1 {
      font-size: var(--fs-title);
      font-weight: 700;
    }
    p {
      font-size: var(--fs-caption);
    }
    @media (max-width: 639.98px) {
      .head {
        flex-wrap: wrap;
      }
    }
    @media (min-width: 1024px) {
      h1,
      p {
        display: none;
      }
    }
  `,
})
export class PageHeader {
  readonly title = input('');
  readonly subtitle = input('');
}

import { Component, ElementRef, computed, inject, signal } from '@angular/core';
import { AuthService } from '../core/auth/auth';
import { SessionActions } from '../core/auth/session-actions';
import { TranslatePipe } from '../core/i18n/translate';
import { Icon } from './icon';

@Component({
  selector: 'app-user-menu',
  imports: [TranslatePipe, Icon],
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'open.set(false)',
  },
  template: `
    <button
      type="button"
      class="trigger"
      [attr.aria-expanded]="open()"
      [title]="'Web.Profile' | t"
      (click)="open.set(!open())"
    >
      <span class="avatar">{{ initials() }}</span>
      <span class="who">
        <strong>{{ auth.session()?.username }}</strong>
        <small>{{ auth.session()?.role }}</small>
      </span>
      <app-icon name="chevron" [size]="16" />
    </button>

    @if (open()) {
      <div class="menu">
        <div class="identity">
          <span class="avatar">{{ initials() }}</span>
          <div>
            <strong>{{ auth.session()?.username }}</strong>
            <small class="muted">{{ auth.session()?.role }}</small>
            @if (auth.branchName()) {
              <small class="muted">{{ auth.branchName() }}</small>
            }
          </div>
        </div>

        <label class="row" for="menu-lang">
          <span>{{ 'Web.Language' | t }}</span>
          <select
            id="menu-lang"
            class="picker"
            [value]="actions.translate.lang()"
            (change)="actions.setLang($any($event.target).value)"
          >
            @for (lang of actions.languages; track lang.code) {
              <option [value]="lang.code">{{ lang.label }}</option>
            }
          </select>
        </label>

        <label class="row" for="menu-theme">
          <span>{{ 'Web.Theme' | t }}</span>
          <select
            id="menu-theme"
            class="picker"
            [value]="actions.theme.mode()"
            (change)="actions.setTheme($any($event.target).value)"
          >
            @for (mode of actions.themes; track mode) {
              <option [value]="mode">{{ actions.themeKey[mode] | t }}</option>
            }
          </select>
        </label>

        <button type="button" class="btn btn-danger btn-block" (click)="logout()">
          <app-icon name="logout" />
          {{ 'Nav.Logout' | t }}
        </button>
      </div>
    }
  `,
  styles: `
    :host {
      position: relative;
      display: inline-flex;
    }
    .trigger {
      display: inline-flex;
      align-items: center;
      gap: var(--sp-sm);
      min-height: var(--touch);
      padding: 0 var(--sp-sm);
      border: none;
      border-radius: var(--r-pill);
      background: transparent;
      color: var(--header-text);
    }
    .trigger:hover {
      background: var(--nav-item-hover-bg);
    }
    .avatar {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      flex: 0 0 32px;
      border-radius: var(--r-pill);
      background: var(--brand);
      color: var(--on-brand);
      font-size: var(--fs-caption);
      font-weight: 700;
    }
    .who,
    .trigger app-icon {
      display: none;
    }
    .who strong {
      display: block;
      font-size: var(--fs-caption);
      line-height: 1.2;
    }
    .who small {
      color: var(--text-2);
      font-size: var(--fs-caption);
    }
    .menu {
      position: absolute;
      inset-inline-end: 0;
      top: calc(100% + var(--sp-xs));
      z-index: 60;
      width: 260px;
      display: flex;
      flex-direction: column;
      gap: var(--sp-md);
      padding: var(--sp-lg);
      border: 1px solid var(--border);
      border-radius: var(--r-lg);
      background: var(--surface);
      box-shadow: var(--shadow-popup);
    }
    .identity {
      display: flex;
      align-items: center;
      gap: var(--sp-md);
    }
    .identity strong {
      display: block;
      word-break: break-word;
    }
    .identity small {
      display: block;
      font-size: var(--fs-caption);
    }
    .row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--sp-md);
    }
    .picker {
      min-height: 38px;
      border: 1px solid var(--border);
      border-radius: var(--r-md);
      background: var(--surface);
      padding: 0 var(--sp-sm);
    }
    @media (min-width: 640px) {
      .who,
      .trigger app-icon {
        display: block;
        text-align: start;
      }
    }
    @media (min-width: 1024px) {
      .trigger {
        min-height: 32px;
      }
    }
  `,
})
export class UserMenu {
  private readonly host = inject(ElementRef<HTMLElement>);
  protected readonly auth = inject(AuthService);
  protected readonly actions = inject(SessionActions);

  protected readonly open = signal(false);
  protected readonly initials = computed(() =>
    (this.auth.session()?.username ?? '?').trim().slice(0, 2).toUpperCase(),
  );

  protected onDocumentClick(event: Event): void {
    if (!this.host.nativeElement.contains(event.target as Node)) this.open.set(false);
  }

  protected async logout(): Promise<void> {
    this.open.set(false);
    await this.actions.logout();
  }
}

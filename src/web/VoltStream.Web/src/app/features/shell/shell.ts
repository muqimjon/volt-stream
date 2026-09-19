import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SessionActions } from '../../core/auth/session-actions';
import { TranslatePipe } from '../../core/i18n/translate';
import { PermissionService } from '../../core/permissions/permission';
import { ThemeMode } from '../../core/theme/theme';
import { PwaService } from '../../core/pwa/pwa';
import { BranchSwitcher } from '../../shared/branch-switcher';
import { Icon } from '../../shared/icon';
import { UserMenu } from '../../shared/user-menu';
import { NAV_ITEMS } from './nav';

const THEME_ORDER: ThemeMode[] = ['system', 'light', 'dark'];

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslatePipe,
    Icon,
    BranchSwitcher,
    UserMenu,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  private readonly permissions = inject(PermissionService);
  private readonly router = inject(Router);
  protected readonly pwa = inject(PwaService);
  protected readonly actions = inject(SessionActions);

  protected readonly items = computed(() =>
    NAV_ITEMS.filter((i) => !i.permission || this.permissions.has(i.permission)),
  );

  protected readonly activeTitle = signal(this.titleFor(this.router.url));

  constructor() {
    const sub = this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) this.activeTitle.set(this.titleFor(event.urlAfterRedirects));
    });
    inject(DestroyRef).onDestroy(() => sub.unsubscribe());
  }

  protected cycleTheme(): void {
    const mode = this.actions.theme.mode();
    const next = THEME_ORDER[(THEME_ORDER.indexOf(mode) + 1) % THEME_ORDER.length];
    this.actions.setTheme(next);
  }

  private titleFor(url: string): string {
    return NAV_ITEMS.find((i) => url.startsWith(i.path))?.key ?? '';
  }
}

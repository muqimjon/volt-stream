import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate';
import { PermissionService } from '../../core/permissions/permission';
import { PwaService } from '../../core/pwa/pwa';
import { ThemeService } from '../../core/theme/theme';
import { BranchSwitcher } from '../../shared/branch-switcher';
import { Icon } from '../../shared/icon';
import { NAV_ITEMS } from './nav';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe, Icon, BranchSwitcher],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  private readonly permissions = inject(PermissionService);
  protected readonly theme = inject(ThemeService);
  protected readonly pwa = inject(PwaService);

  protected readonly items = computed(() =>
    NAV_ITEMS.filter((i) => !i.permission || this.permissions.has(i.permission)),
  );

  protected readonly themeIcon = computed(() => (this.theme.mode() === 'dark' ? 'moon' : 'sun'));

  protected toggleTheme(): void {
    const order = ['system', 'light', 'dark'] as const;
    const next = order[(order.indexOf(this.theme.mode()) + 1) % order.length];
    this.theme.set(next);
  }
}

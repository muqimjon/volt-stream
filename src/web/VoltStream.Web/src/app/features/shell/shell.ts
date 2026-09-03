import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate';
import { PermissionService } from '../../core/permissions/permission';
import { PwaService } from '../../core/pwa/pwa';
import { BranchSwitcher } from '../../shared/branch-switcher';
import { Icon } from '../../shared/icon';
import { UserMenu } from '../../shared/user-menu';
import { NAV_ITEMS } from './nav';

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
  protected readonly pwa = inject(PwaService);

  protected readonly items = computed(() =>
    NAV_ITEMS.filter((i) => !i.permission || this.permissions.has(i.permission)),
  );
}

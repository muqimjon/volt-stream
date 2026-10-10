import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import {
  NavigationEnd,
  NavigationSkipped,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { SessionActions } from '../../core/auth/session-actions';
import { TranslatePipe } from '../../core/i18n/translate';
import { ThemeMode } from '../../core/theme/theme';
import { PwaService } from '../../core/pwa/pwa';
import { closeOnBack } from '../../core/ui/back-stack';
import { BreakpointService } from '../../core/ui/breakpoint';
import { KeyboardService } from '../../core/ui/keyboard';
import { PageTitle } from '../../core/ui/page-title';
import { PaletteService } from '../../core/ui/palette';
import { BranchSwitcher } from '../../shared/branch-switcher';
import { Icon } from '../../shared/icon';
import { UserMenu } from '../../shared/user-menu';
import { CommandPalette } from './command-palette';
import { BottomNavService } from './bottom-nav';
import { NAV_ITEMS } from './nav';

const THEME_ORDER: ThemeMode[] = ['system', 'light', 'dark'];
const SIDEBAR_KEY = 'vs.sidebar';

function readCollapsed(): boolean {
  try {
    return localStorage.getItem(SIDEBAR_KEY) === 'collapsed';
  } catch {
    return false;
  }
}

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
    CommandPalette,
  ],
  host: { '(document:keydown)': 'onKey($event)' },
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  protected readonly nav = inject(BottomNavService);
  private readonly router = inject(Router);
  protected readonly pwa = inject(PwaService);
  protected readonly actions = inject(SessionActions);
  protected readonly palette = inject(PaletteService);
  protected readonly title = inject(PageTitle);
  private readonly keyboard = inject(KeyboardService);
  private readonly bp = inject(BreakpointService);

  protected readonly items = this.nav.allowed;

  protected readonly activeTitle = signal(this.titleFor(this.router.url));

  private readonly collapsed = signal(readCollapsed());
  protected readonly railOpen = signal(false);
  protected readonly moreOpen = signal(false);
  private readonly url = signal(this.router.url);
  protected readonly moreActive = computed(() =>
    this.nav.more().some((i) => this.url().startsWith(i.path)),
  );
  protected readonly compact = computed(() =>
    this.bp.desktop() ? this.collapsed() : !this.railOpen(),
  );

  constructor() {
    closeOnBack(
      () => this.moreOpen(),
      () => this.moreOpen.set(false),
    );
    const sub = this.router.events.subscribe((event) => {
      if (event instanceof NavigationSkipped) this.moreOpen.set(false);
      if (event instanceof NavigationEnd) {
        this.activeTitle.set(this.titleFor(event.urlAfterRedirects));
        this.railOpen.set(false);
        this.moreOpen.set(false);
        this.url.set(event.urlAfterRedirects);
      }
    });
    inject(DestroyRef).onDestroy(() => sub.unsubscribe());
  }

  protected toggleSidebar(): void {
    if (!this.bp.desktop()) return this.railOpen.update((open) => !open);
    const collapsed = !this.collapsed();
    this.collapsed.set(collapsed);
    try {
      localStorage.setItem(SIDEBAR_KEY, collapsed ? 'collapsed' : 'expanded');
    } catch {}
  }

  protected onKey(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.railOpen.set(false);
      this.moreOpen.set(false);
    } else if ((event.ctrlKey || event.metaKey) && event.code === 'KeyK') {
      event.preventDefault();
      this.palette.toggle();
    }
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

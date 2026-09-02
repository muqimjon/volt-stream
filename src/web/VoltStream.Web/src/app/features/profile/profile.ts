import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth';
import { LANGUAGES, LangCode, TranslatePipe, TranslateService } from '../../core/i18n/translate';
import { PwaService } from '../../core/pwa/pwa';
import { ThemeService, ThemeMode } from '../../core/theme/theme';
import { DialogService } from '../../core/ui/dialog';
import { APP_VERSION } from '../../core/version';
import { Icon } from '../../shared/icon';
import { PageHeader } from '../../shared/page-header';

@Component({
  selector: 'app-profile',
  imports: [TranslatePipe, PageHeader, Icon],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class ProfilePage {
  private readonly router = inject(Router);
  private readonly dialog = inject(DialogService);

  protected readonly auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);
  protected readonly pwa = inject(PwaService);
  protected readonly translate = inject(TranslateService);

  protected readonly languages = LANGUAGES;
  protected readonly themes: ThemeMode[] = ['system', 'light', 'dark'];
  protected readonly themeKey: Record<ThemeMode, string> = {
    system: 'Web.ThemeSystem',
    light: 'Web.ThemeLight',
    dark: 'Web.ThemeDark',
  };
  protected readonly version = APP_VERSION;

  protected setLang(code: string): void {
    void this.translate.use(code as LangCode);
  }

  protected setTheme(mode: string): void {
    this.theme.set(mode as ThemeMode);
  }

  protected async logout(): Promise<void> {
    if (!(await this.dialog.confirm(this.translate.t('Web.LogoutConfirm'), { danger: true })))
      return;
    this.auth.logout();
    await this.router.navigate(['/login']);
  }
}

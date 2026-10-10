import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { LANGUAGES, LangCode, TranslateService } from '../i18n/translate';
import { ThemeMode, ThemeService } from '../theme/theme';
import { DialogService } from '../ui/dialog';
import { AuthService } from './auth';

@Injectable({ providedIn: 'root' })
export class SessionActions {
  private readonly router = inject(Router);
  private readonly dialog = inject(DialogService);
  private readonly auth = inject(AuthService);

  readonly theme = inject(ThemeService);
  readonly translate = inject(TranslateService);

  readonly languages = LANGUAGES;
  readonly themes: ThemeMode[] = ['system', 'light', 'dark'];
  readonly themeKey: Record<ThemeMode, string> = {
    system: 'Web.ThemeSystem',
    light: 'Web.ThemeLight',
    dark: 'Web.ThemeDark',
  };

  setLang(code: string): void {
    void this.translate.use(code as LangCode);
  }

  setTheme(mode: string): void {
    this.theme.set(mode as ThemeMode);
  }

  async logout(): Promise<void> {
    // The login page is lazy: fetch it before the session disappears so a slow link never shows a blank profile.
    const login = import('../../features/login/login').catch(() => undefined);
    if (!(await this.dialog.confirm(this.translate.t('Web.LogoutConfirm'), { danger: true })))
      return;
    await login;
    this.auth.logout();
    await this.router.navigate(['/login'], { replaceUrl: true });
  }
}

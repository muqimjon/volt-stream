import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiError } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth';
import { LANGUAGES, LangCode, TranslatePipe, TranslateService } from '../../core/i18n/translate';

@Component({
  selector: 'app-login',
  imports: [TranslatePipe],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly translate = inject(TranslateService);
  protected readonly languages = LANGUAGES;

  protected readonly username = signal('');
  protected readonly password = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal('');

  protected async submit(event: Event): Promise<void> {
    event.preventDefault();
    if (this.busy()) return;

    const username = this.username().trim();
    if (!username) return this.error.set(this.translate.t('Login.EnterUsername'));
    if (!this.password()) return this.error.set(this.translate.t('Login.EnterPassword'));

    this.error.set('');
    this.busy.set(true);
    try {
      await this.auth.login(username, this.password());
      await this.router.navigate(['/']);
    } catch (e) {
      const err = e as ApiError;
      this.error.set(
        err.status === 401 || err.status === 400
          ? this.translate.t('Login.InvalidCredentials')
          : err.message,
      );
    } finally {
      this.busy.set(false);
    }
  }

  protected setLang(code: string): void {
    void this.translate.use(code as LangCode);
  }
}

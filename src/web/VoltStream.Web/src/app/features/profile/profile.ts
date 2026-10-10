import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth';
import { SessionActions } from '../../core/auth/session-actions';
import { TranslatePipe } from '../../core/i18n/translate';
import { PwaService } from '../../core/pwa/pwa';
import { APP_VERSION } from '../../core/version';
import { Icon } from '../../shared/icon';
import { PageHeader } from '../../shared/page-header';
import { BottomNavService, MAX_PINNED } from '../shell/bottom-nav';

@Component({
  selector: 'app-profile',
  imports: [TranslatePipe, PageHeader, Icon],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class ProfilePage {
  protected readonly auth = inject(AuthService);
  protected readonly pwa = inject(PwaService);
  protected readonly actions = inject(SessionActions);
  protected readonly nav = inject(BottomNavService);
  protected readonly max = MAX_PINNED;
  protected readonly version = APP_VERSION;
}

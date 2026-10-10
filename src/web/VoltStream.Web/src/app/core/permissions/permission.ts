import {
  Directive,
  Injectable,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  input,
} from '@angular/core';
import { AuthService } from '../auth/auth';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly auth = inject(AuthService);

  has(permission: string | string[]): boolean {
    if (this.auth.isAdmin()) return true;
    const granted = this.auth.permissions();
    return [permission].flat().some((p) => granted.includes(p));
  }
}

@Directive({ selector: '[appHasPermission]' })
export class HasPermissionDirective {
  readonly appHasPermission = input.required<string>();

  private readonly permissions = inject(PermissionService);
  private readonly template = inject(TemplateRef<unknown>);
  private readonly container = inject(ViewContainerRef);
  private rendered = false;

  constructor() {
    effect(() => {
      const allowed = this.permissions.has(this.appHasPermission());
      if (allowed === this.rendered) return;
      this.rendered = allowed;
      this.container.clear();
      if (allowed) this.container.createEmbeddedView(this.template);
    });
  }
}

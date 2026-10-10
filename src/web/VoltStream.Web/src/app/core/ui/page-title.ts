import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PageTitle {
  readonly override = signal<string | null>(null);
}

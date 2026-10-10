import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PaletteService {
  readonly open = signal(false);

  toggle(): void {
    this.open.update((open) => !open);
  }
}

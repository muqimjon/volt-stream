import { Injectable, effect, signal } from '@angular/core';

export type ThemeMode = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'vs.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<ThemeMode>(read());
  private readonly media = matchMedia('(prefers-color-scheme: dark)');
  private readonly systemDark = signal(this.media.matches);

  constructor() {
    this.media.addEventListener('change', (e) => this.systemDark.set(e.matches));
    effect(() => {
      const mode = this.mode();
      const root = document.documentElement;
      if (mode === 'system') delete root.dataset['theme'];
      else root.dataset['theme'] = mode;
      const dark = mode === 'dark' || (mode === 'system' && this.systemDark());
      document
        .querySelector('meta[name="theme-color"]')
        ?.setAttribute('content', dark ? '#141A2A' : '#4F46E5');
    });
  }

  set(mode: ThemeMode): void {
    this.mode.set(mode);
    localStorage.setItem(STORAGE_KEY, mode);
  }
}

function read(): ThemeMode {
  const v = localStorage.getItem(STORAGE_KEY);
  return v === 'light' || v === 'dark' || v === 'system' ? v : 'system';
}

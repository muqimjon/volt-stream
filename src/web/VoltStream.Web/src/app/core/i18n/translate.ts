import { Injectable, Pipe, PipeTransform, computed, inject, signal } from '@angular/core';

export type LangCode = 'uz-Latn' | 'uz-Cyrl' | 'ru' | 'en';

export const LANGUAGES: { code: LangCode; label: string }[] = [
  { code: 'uz-Latn', label: "O'zbekcha" },
  { code: 'uz-Cyrl', label: 'Ўзбекча' },
  { code: 'ru', label: 'Русский' },
  { code: 'en', label: 'English' },
];

const FALLBACK: LangCode = 'uz-Latn';
const STORAGE_KEY = 'vs.lang';

type Dict = Record<string, string>;

@Injectable({ providedIn: 'root' })
export class TranslateService {
  private readonly dicts = signal<Partial<Record<LangCode, Dict>>>({});
  readonly lang = signal<LangCode>(readStoredLang());

  private readonly active = computed(() => this.dicts()[this.lang()] ?? {});
  private readonly fallback = computed(() => this.dicts()[FALLBACK] ?? {});

  async init(): Promise<void> {
    await this.ensure(FALLBACK);
    if (this.lang() !== FALLBACK) await this.ensure(this.lang());
  }

  async use(code: LangCode): Promise<void> {
    await this.ensure(code);
    this.lang.set(code);
    localStorage.setItem(STORAGE_KEY, code);
    document.documentElement.lang = code.startsWith('uz') ? 'uz' : code;
  }

  t(key: string, ...args: unknown[]): string {
    const raw = this.active()[key] ?? this.fallback()[key];
    if (raw === undefined) return key;
    if (!args.length) return raw;
    return raw.replace(/\{(\d+)(?::[^}]*)?\}/g, (m, i) => {
      const v = args[Number(i)];
      return v === null || v === undefined ? m : String(v);
    });
  }

  private async ensure(code: LangCode): Promise<void> {
    if (this.dicts()[code]) return;
    try {
      const res = await fetch(`i18n/${code}.json`, { cache: 'no-cache' });
      if (!res.ok) return;
      const dict = (await res.json()) as Dict;
      this.dicts.update((d) => ({ ...d, [code]: dict }));
    } catch {
      /* keep fallback */
    }
  }
}

function readStoredLang(): LangCode {
  const stored = localStorage.getItem(STORAGE_KEY) as LangCode | null;
  if (stored && LANGUAGES.some((l) => l.code === stored)) return stored;
  const nav = navigator.language.toLowerCase();
  if (nav.startsWith('ru')) return 'ru';
  if (nav.startsWith('en')) return 'en';
  return FALLBACK;
}

@Pipe({ name: 't', pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly translate = inject(TranslateService);

  transform(key: string, ...args: unknown[]): string {
    return this.translate.t(key, ...args);
  }
}

import { Injectable, signal } from '@angular/core';

export interface DialogChoice {
  id: number;
  label: string;
}

export interface ConfirmRequest {
  message: string;
  title: string;
  yesKey: string;
  noKey: string;
  danger: boolean;
  choices?: DialogChoice[];
  selectedId?: number | null;
  resolve: (ok: boolean, choice?: number) => void;
}

@Injectable({ providedIn: 'root' })
export class DialogService {
  readonly request = signal<ConfirmRequest | null>(null);

  confirm(
    message: string,
    options: { title?: string; yesKey?: string; noKey?: string; danger?: boolean } = {},
  ): Promise<boolean> {
    return new Promise((resolve) =>
      this.request.set({
        message,
        title: options.title ?? 'Sales.ConfirmTitle',
        yesKey: options.yesKey ?? 'Common.Yes',
        noKey: options.noKey ?? 'Common.No',
        danger: options.danger ?? false,
        resolve,
      }),
    );
  }

  choose(
    title: string,
    choices: DialogChoice[],
    selectedId: number | null,
  ): Promise<number | null> {
    return new Promise((resolve) =>
      this.request.set({
        message: '',
        title,
        yesKey: 'Common.Yes',
        noKey: 'Common.No',
        danger: false,
        choices,
        selectedId,
        resolve: (ok, choice) => resolve(ok && choice !== undefined ? choice : null),
      }),
    );
  }

  answer(ok: boolean, choice?: number): void {
    const req = this.request();
    this.request.set(null);
    req?.resolve(ok, choice);
  }
}

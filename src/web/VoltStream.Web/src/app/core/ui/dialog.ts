import { Injectable, signal } from '@angular/core';

export interface ConfirmRequest {
  message: string;
  title: string;
  yesKey: string;
  noKey: string;
  danger: boolean;
  resolve: (ok: boolean) => void;
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

  answer(ok: boolean): void {
    const req = this.request();
    this.request.set(null);
    req?.resolve(ok);
  }
}

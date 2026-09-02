import { Injectable, inject, signal } from '@angular/core';
import { SwUpdate } from '@angular/service-worker';

interface InstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

@Injectable({ providedIn: 'root' })
export class PwaService {
  private readonly updates = inject(SwUpdate);

  readonly updateReady = signal(false);
  readonly canInstall = signal(false);
  readonly online = signal(navigator.onLine);

  private deferredPrompt: InstallPromptEvent | null = null;

  constructor() {
    addEventListener('online', () => this.online.set(true));
    addEventListener('offline', () => this.online.set(false));

    addEventListener('beforeinstallprompt', (event) => {
      event.preventDefault();
      this.deferredPrompt = event as InstallPromptEvent;
      this.canInstall.set(true);
    });

    addEventListener('appinstalled', () => {
      this.deferredPrompt = null;
      this.canInstall.set(false);
    });

    if (this.updates.isEnabled) {
      this.updates.versionUpdates.subscribe((event) => {
        if (event.type === 'VERSION_READY') this.updateReady.set(true);
      });
    }
  }

  async install(): Promise<void> {
    const prompt = this.deferredPrompt;
    if (!prompt) return;
    this.deferredPrompt = null;
    this.canInstall.set(false);
    await prompt.prompt();
    await prompt.userChoice;
  }

  async applyUpdate(): Promise<void> {
    if (this.updates.isEnabled) await this.updates.activateUpdate();
    location.reload();
  }
}

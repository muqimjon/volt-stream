import { Injectable, effect, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { AuthService } from '../auth/auth';
import { PrintJob } from '../models';

@Injectable({ providedIn: 'root' })
export class PrintHubService {
  private readonly auth = inject(AuthService);

  readonly connected = signal(false);
  readonly bySale = signal<Record<number, PrintJob>>({});

  private connection: HubConnection | null = null;
  private startedFor: string | null = null;

  constructor() {
    effect(() => {
      const token = this.auth.token();
      if (token) void this.start(token);
      else void this.stop();
    });

    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') void this.resume();
    });
  }

  track(job: PrintJob | null | undefined): void {
    if (!job) return;
    this.bySale.update((map) => ({ ...map, [job.saleId]: job }));
  }

  jobFor(saleId: number): PrintJob | null {
    return this.bySale()[saleId] ?? null;
  }

  private async start(token: string): Promise<void> {
    if (this.startedFor === token && this.connection) return;
    await this.stop();
    this.startedFor = token;

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/print', { accessTokenFactory: () => this.auth.token() ?? '' })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('PrintJobStatusChanged', (job: PrintJob) => this.track(job));
    connection.onreconnected(() => this.connected.set(true));
    connection.onreconnecting(() => this.connected.set(false));
    connection.onclose(() => this.connected.set(false));

    this.connection = connection;
    try {
      await connection.start();
      this.connected.set(true);
    } catch {
      this.connected.set(false);
    }
  }

  private async stop(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.startedFor = null;
    this.connected.set(false);
    if (connection) await connection.stop().catch(() => undefined);
  }

  private async resume(): Promise<void> {
    const token = this.auth.token();
    if (!token) return;
    if (this.connection && this.connection.state === HubConnectionState.Disconnected) {
      try {
        await this.connection.start();
        this.connected.set(true);
      } catch {
        this.connected.set(false);
      }
    } else if (!this.connection) {
      await this.start(token);
    }
  }
}

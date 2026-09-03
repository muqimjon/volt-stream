import { Injectable, Signal, signal } from '@angular/core';

function matches(query: string): Signal<boolean> {
  const mq = window.matchMedia(query);
  const state = signal(mq.matches);
  mq.addEventListener('change', (e) => state.set(e.matches));
  return state.asReadonly();
}

@Injectable({ providedIn: 'root' })
export class BreakpointService {
  readonly desktop = matches('(min-width: 1024px)');
}

import { Injectable, effect, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'rsaas.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<ThemeMode>((localStorage.getItem(STORAGE_KEY) as ThemeMode) ?? 'system');

  private readonly media = window.matchMedia?.('(prefers-color-scheme: dark)');

  constructor() {
    effect(() => this.apply(this.mode()));
    this.media?.addEventListener?.('change', () => {
      if (this.mode() === 'system') this.apply('system');
    });
  }

  setMode(mode: ThemeMode): void {
    localStorage.setItem(STORAGE_KEY, mode);
    this.mode.set(mode);
  }

  toggle(): void {
    const resolved = this.resolvedIsDark();
    this.setMode(resolved ? 'light' : 'dark');
  }

  resolvedIsDark(): boolean {
    const mode = this.mode();
    return mode === 'dark' || (mode === 'system' && !!this.media?.matches);
  }

  private apply(mode: ThemeMode): void {
    const isDark = mode === 'dark' || (mode === 'system' && !!this.media?.matches);
    document.documentElement.classList.toggle('dark-theme', isDark);
    document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
  }
}

import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ThemeService } from './theme.service';

/**
 * Header control that flips between light and dark. It holds no state of its own — it reads and
 * toggles the ThemeService. The icon shows the mode you'd switch *to*, matching the aria-label.
 */
@Component({
  selector: 'app-theme-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="toggle"
      (click)="theme.toggle()"
      [attr.aria-pressed]="isDark()"
      [attr.aria-label]="label()"
      [title]="label()"
    >
      {{ isDark() ? '☀️' : '🌙' }}
    </button>
  `,
  styles: `
    .toggle {
      display: inline-flex; align-items: center; justify-content: center;
      width: 2rem; height: 2rem; padding: 0; font-size: 1rem; line-height: 1;
      background: none; color: inherit; border: 1px solid var(--border-strong);
      border-radius: 6px; cursor: pointer;
    }
    .toggle:hover { background: var(--hover-bg); }
  `,
})
export class ThemeToggle {
  protected readonly theme = inject(ThemeService);
  protected readonly isDark = computed(() => this.theme.theme() === 'dark');
  protected readonly label = computed(() => `Switch to ${this.isDark() ? 'light' : 'dark'} mode`);
}

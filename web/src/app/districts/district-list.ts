import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DistrictSummary } from './district.models';

/**
 * Presentational district list: name, primary and store count per row, plus loading and empty
 * states. It owns no data — the container feeds it and listens for a selection.
 */
@Component({
  selector: 'app-district-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <p class="state" role="status">Loading districts…</p>
    } @else if (districts().length === 0) {
      <p class="state">No districts to show.</p>
    } @else {
      <ul class="districts">
        @for (d of districts(); track d.id) {
          <li>
            <button
              type="button"
              class="district"
              [class.selected]="d.id === selectedId()"
              [attr.aria-pressed]="d.id === selectedId()"
              (click)="select.emit(d.id)"
            >
              <span class="name">{{ d.name }}</span>
              <span class="primary">★ {{ d.primary.name }}</span>
              <span class="stores">{{ d.storeCount }} {{ d.storeCount === 1 ? 'store' : 'stores' }}</span>
            </button>
          </li>
        }
      </ul>
    }
  `,
  styles: `
    .districts { list-style: none; margin: 0; padding: 0; display: grid; gap: 0.25rem; }
    .district { display: flex; gap: 0.75rem; align-items: baseline; width: 100%; text-align: left;
      padding: 0.5rem 0.75rem; background: none; border: 1px solid transparent; border-radius: 6px; cursor: pointer; }
    .district:hover { background: rgba(0, 0, 0, 0.04); }
    .district.selected { border-color: currentColor; font-weight: 600; }
    .name { flex: 1; }
    .primary, .stores { color: #555; font-size: 0.9em; }
    .state { color: #666; }
  `,
})
export class DistrictList {
  readonly districts = input.required<DistrictSummary[]>();
  readonly loading = input(false);
  readonly selectedId = input<number | null>(null);
  readonly select = output<number>();
}

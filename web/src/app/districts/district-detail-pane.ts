import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DistrictDetail } from './district.models';

/**
 * Presentational detail pane: the selected district's primary, secondaries and stores, each with its
 * own empty state. It owns no data — the container passes the loaded detail (or null when nothing is
 * selected yet).
 */
@Component({
  selector: 'app-district-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loading()) {
      <p class="state" role="status">Loading…</p>
    } @else if (detail(); as d) {
      <h2 class="name">{{ d.name }}</h2>

      <section>
        <h3>Primary</h3>
        <p class="primary">★ {{ d.primary.name }}</p>
      </section>

      <section>
        <h3>Secondaries</h3>
        @if (d.secondaries.length === 0) {
          <p class="state">No secondaries.</p>
        } @else {
          <ul class="chips">
            @for (s of d.secondaries; track s.id) {
              <li>{{ s.name }}</li>
            }
          </ul>
        }
      </section>

      <section>
        <h3>Stores</h3>
        @if (d.stores.length === 0) {
          <p class="state">No stores.</p>
        } @else {
          <ul class="chips">
            @for (st of d.stores; track st.id) {
              <li>{{ st.name }}</li>
            }
          </ul>
        }
      </section>
    } @else {
      <p class="state">Select a district to see its coverage.</p>
    }
  `,
  styles: `
    .name { margin-top: 0; }
    section { margin-bottom: 1rem; }
    h3 { margin: 0 0 0.35rem; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.03em; color: #555; }
    .primary { margin: 0; font-weight: 600; }
    .chips { list-style: none; margin: 0; padding: 0; display: flex; flex-wrap: wrap; gap: 0.4rem; }
    .chips li { padding: 0.2rem 0.6rem; border: 1px solid rgba(0, 0, 0, 0.15); border-radius: 999px; }
    .state { color: #666; }
  `,
})
export class DistrictDetailPane {
  readonly detail = input<DistrictDetail | null>(null);
  readonly loading = input(false);
}

import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { DistrictDetail, Salesperson } from './district.models';

/**
 * Presentational detail pane for the selected district: primary, secondaries and stores, each with
 * an empty state, plus the mutation controls. It holds no server state and performs no requests — it
 * emits intents (add / remove secondary, change primary) that the container turns into a PUT.
 *
 * The primary has no "remove" control (a district must always keep exactly one — BR-4); the only way
 * to change it is to make a secondary the primary, which is gated behind an explaining confirmation.
 */
@Component({
  selector: 'app-district-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (error(); as e) {
      <p class="error" role="alert">{{ e }}</p>
    }
    @if (saving()) {
      <p class="saving" role="status" aria-live="polite">Saving…</p>
    }

    @if (loading()) {
      <p class="state" role="status">Loading…</p>
    } @else if (detail(); as d) {
      <h2 class="name">{{ d.name }}</h2>

      <section>
        <h3>Primary</h3>
        <p class="primary">★ {{ d.primary.name }}</p>
        <p class="hint">The primary can’t be removed — make a secondary the primary to replace them.</p>
      </section>

      <section>
        <h3>Secondaries</h3>
        @if (d.secondaries.length === 0) {
          <p class="state">No secondaries.</p>
        } @else {
          <ul class="rows">
            @for (s of d.secondaries; track s.id) {
              <li>
                <span class="who">{{ s.name }}</span>
                <button type="button" class="link make-primary-btn" [disabled]="saving()"
                        (click)="confirmingId.set(s.id)">make primary</button>
                <button type="button" class="link danger remove-btn" [disabled]="saving()"
                        (click)="removeSecondary.emit(s.id)">remove</button>

                @if (confirmingId() === s.id) {
                  <div class="confirm" role="alertdialog" aria-label="Confirm change of primary">
                    <p>
                      Make {{ s.name }} the primary of {{ d.name }}?
                      {{ d.primary.name }} will step down and no longer cover this district.
                    </p>
                    <button type="button" class="confirm-btn" (click)="onConfirmPrimary(s.id)">Confirm</button>
                    <button type="button" class="cancel-btn" (click)="confirmingId.set(null)">Cancel</button>
                  </div>
                }
              </li>
            }
          </ul>
        }

        <div class="add">
          <label>
            Add secondary
            <select #sel [disabled]="saving() || available().length === 0"
                    (change)="selectedToAdd.set(sel.value ? +sel.value : null)">
              <option value="">Choose…</option>
              @for (s of available(); track s.id) {
                <option [value]="s.id">{{ s.name }}</option>
              }
            </select>
          </label>
          <button type="button" class="add-btn" [disabled]="selectedToAdd() === null || saving()"
                  (click)="onAdd()">Add</button>
        </div>
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
    h3 { margin: 0 0 0.35rem; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.03em; color: var(--muted); }
    .primary { margin: 0; font-weight: 600; }
    .hint { margin: 0.25rem 0 0; font-size: 0.8rem; color: var(--muted); }
    .rows { list-style: none; margin: 0 0 0.5rem; padding: 0; display: grid; gap: 0.35rem; }
    .rows li { display: flex; flex-wrap: wrap; align-items: baseline; gap: 0.5rem; }
    .who { flex: 1; }
    .link { background: none; border: none; padding: 0; color: var(--link); cursor: pointer; font: inherit; }
    .link.danger { color: var(--danger); }
    .link:disabled { color: var(--link-disabled); cursor: default; }
    .confirm { flex-basis: 100%; margin-top: 0.25rem; padding: 0.5rem 0.75rem; border: 1px solid var(--confirm-border);
      background: var(--confirm-bg); border-radius: 6px; }
    .confirm p { margin: 0 0 0.5rem; }
    .add { display: flex; gap: 0.5rem; align-items: end; }
    .chips { list-style: none; margin: 0; padding: 0; display: flex; flex-wrap: wrap; gap: 0.4rem; }
    .chips li { padding: 0.2rem 0.6rem; border: 1px solid var(--border-strong); border-radius: 999px; }
    .state { color: var(--muted-2); }
    .error { margin: 0 0 0.75rem; padding: 0.5rem 0.75rem; border: 1px solid var(--error-border);
      background: var(--error-bg); color: var(--error-fg); border-radius: 6px; }
    .saving { margin: 0 0 0.5rem; color: var(--link); font-size: 0.85rem; }
  `,
})
export class DistrictDetailPane {
  readonly detail = input<DistrictDetail | null>(null);
  readonly loading = input(false);
  readonly saving = input(false);
  readonly error = input<string | null>(null);
  readonly salespersons = input<Salesperson[]>([]);

  readonly addSecondary = output<number>();
  readonly removeSecondary = output<number>();
  readonly makePrimary = output<number>();

  protected readonly confirmingId = signal<number | null>(null);
  protected readonly selectedToAdd = signal<number | null>(null);

  /** Salespersons not already the primary or a secondary of this district. */
  protected readonly available = computed(() => {
    const d = this.detail();
    if (!d) return [];
    const taken = new Set<number>([d.primary.id, ...d.secondaries.map((s) => s.id)]);
    return this.salespersons().filter((s) => !taken.has(s.id));
  });

  protected onAdd(): void {
    const id = this.selectedToAdd();
    if (id !== null) {
      this.addSecondary.emit(id);
      this.selectedToAdd.set(null);
    }
  }

  protected onConfirmPrimary(id: number): void {
    this.makePrimary.emit(id);
    this.confirmingId.set(null);
  }
}

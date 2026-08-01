import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { DistrictDetailPane } from './districts/district-detail-pane';
import { DistrictList } from './districts/district-list';
import { DistrictDetail, DistrictSummary, Salesperson } from './districts/district.models';
import { DistrictService } from './districts/district.service';

@Component({
  selector: 'app-root',
  imports: [DistrictList, DistrictDetailPane],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly districtService = inject(DistrictService);

  protected readonly districts = signal<DistrictSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly selectedId = signal<number | null>(null);
  protected readonly detail = signal<DistrictDetail | null>(null);
  protected readonly detailLoading = signal(false);
  protected readonly salespersons = signal<Salesperson[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadList();
    this.districtService.salespersons().subscribe({ next: (people) => this.salespersons.set(people) });
  }

  protected onSelect(id: number): void {
    this.selectedId.set(id);
    this.detail.set(null);
    this.error.set(null);
    this.detailLoading.set(true);
    this.districtService.detail(id).subscribe({
      next: (detail) => {
        this.detail.set(detail);
        this.detailLoading.set(false);
      },
      error: (err) => {
        this.error.set(this.toMessage(err));
        this.detailLoading.set(false);
      },
    });
  }

  protected onAddSecondary(salespersonId: number): void {
    const d = this.detail();
    if (d) this.replace(d.primary.id, [...this.secondaryIds(d), salespersonId]);
  }

  protected onRemoveSecondary(salespersonId: number): void {
    const d = this.detail();
    if (d) this.replace(d.primary.id, this.secondaryIds(d).filter((id) => id !== salespersonId));
  }

  protected onMakePrimary(salespersonId: number): void {
    const d = this.detail();
    // New primary; drop it from secondaries. The old primary steps down (it is not kept as a secondary).
    if (d) this.replace(salespersonId, this.secondaryIds(d).filter((id) => id !== salespersonId));
  }

  private secondaryIds(detail: DistrictDetail): number[] {
    return detail.secondaries.map((s) => s.id);
  }

  private replace(primaryId: number, secondaryIds: number[]): void {
    const d = this.detail();
    if (!d) return;
    this.error.set(null);
    this.saving.set(true);
    this.districtService
      .putAssignments(d.id, { primaryId, secondaryIds, concurrencyToken: d.concurrencyToken })
      .subscribe({
        next: (updated) => {
          this.detail.set(updated);
          this.saving.set(false);
          this.loadList(); // primary name in the list may have changed
        },
        error: (err) => {
          this.error.set(this.toMessage(err));
          this.saving.set(false);
        },
      });
  }

  /** Pull the human message out of an RFC 9457 ProblemDetails body, with sane fallbacks. */
  private toMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as { detail?: string; title?: string } | null;
      return body?.detail ?? body?.title ?? err.message;
    }
    return 'Something went wrong. Please try again.';
  }

  private loadList(): void {
    this.districtService.list().subscribe({
      next: (districts) => {
        this.districts.set(districts);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}

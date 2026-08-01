import { Component, OnInit, inject, signal } from '@angular/core';
import { DistrictDetailPane } from './districts/district-detail-pane';
import { DistrictList } from './districts/district-list';
import { DistrictDetail, DistrictSummary } from './districts/district.models';
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

  ngOnInit(): void {
    this.districtService.list().subscribe({
      next: (districts) => {
        this.districts.set(districts);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected onSelect(id: number): void {
    this.selectedId.set(id);
    this.detail.set(null);
    this.detailLoading.set(true);
    this.districtService.detail(id).subscribe({
      next: (detail) => {
        this.detail.set(detail);
        this.detailLoading.set(false);
      },
      error: () => this.detailLoading.set(false),
    });
  }
}

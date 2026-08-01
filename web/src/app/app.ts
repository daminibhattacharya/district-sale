import { Component, OnInit, inject, signal } from '@angular/core';
import { DistrictList } from './districts/district-list';
import { DistrictSummary } from './districts/district.models';
import { DistrictService } from './districts/district.service';

@Component({
  selector: 'app-root',
  imports: [DistrictList],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly districtService = inject(DistrictService);

  protected readonly districts = signal<DistrictSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly selectedId = signal<number | null>(null);

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
    this.selectedId.set(id); // detail pane is wired in the next step
  }
}

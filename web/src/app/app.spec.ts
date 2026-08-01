import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { App } from './app';
import { DistrictDetail, DistrictSummary, Salesperson } from './districts/district.models';
import { DistrictService } from './districts/district.service';

describe('App', () => {
  const districts: DistrictSummary[] = [
    { id: 1, name: 'North Denmark', primary: { id: 1, name: 'Anna' }, storeCount: 4 },
  ];

  const detail: DistrictDetail = {
    id: 1,
    name: 'North Denmark',
    primary: { id: 1, name: 'Anna' },
    secondaries: [{ id: 5, name: 'Emil' }],
    stores: [{ id: 101, name: 'Aalborg' }],
    concurrencyToken: 'AAAAAAAAB9E=',
  };

  const pool: Salesperson[] = [
    { id: 1, name: 'Anna' },
    { id: 5, name: 'Emil' },
    { id: 8, name: 'Helle' },
  ];

  function setup() {
    const service = {
      list: vi.fn().mockReturnValue(of(districts)),
      detail: vi.fn().mockReturnValue(of(detail)),
      salespersons: vi.fn().mockReturnValue(of(pool)),
      putAssignments: vi.fn().mockReturnValue(of({ ...detail, secondaries: [], concurrencyToken: 'NEW' })),
    };
    TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: DistrictService, useValue: service }],
    });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return { fixture, service };
  }

  function el(fixture: ComponentFixture<App>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function selectFirstDistrict(fixture: ComponentFixture<App>): Promise<void> {
    await fixture.whenStable();
    el(fixture).querySelector<HTMLButtonElement>('button.district')!.click();
    fixture.detectChanges();
    await fixture.whenStable();
  }

  it('loads districts from the service and renders them', async () => {
    const { fixture } = setup();
    await fixture.whenStable();
    expect(el(fixture).textContent).toContain('North Denmark');
  });

  it('loads and shows the detail when a district is selected', async () => {
    const { fixture } = setup();
    await selectFirstDistrict(fixture);
    expect(el(fixture).textContent).toContain('Emil'); // secondary from the detail
  });

  it('removing a secondary PUTs the reduced assignment set with the concurrency token', async () => {
    const { fixture, service } = setup();
    await selectFirstDistrict(fixture);

    el(fixture).querySelector<HTMLButtonElement>('.remove-btn')!.click();

    expect(service.putAssignments).toHaveBeenCalledWith(1, {
      primaryId: 1,
      secondaryIds: [],
      concurrencyToken: 'AAAAAAAAB9E=',
    });
  });
});

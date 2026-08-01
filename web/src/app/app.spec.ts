import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
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

  it('surfaces the server message when a mutation fails on a stale token (409)', async () => {
    const { fixture, service } = setup();
    service.putAssignments.mockReturnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: { title: 'Conflict', detail: 'The district was changed by someone else. Reload and try again.' },
      })),
    );
    await selectFirstDistrict(fixture);

    el(fixture).querySelector<HTMLButtonElement>('.remove-btn')!.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const alert = el(fixture).querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('changed by someone else');
  });

  it('surfaces the server message when the detail fails to load', async () => {
    const { fixture, service } = setup();
    service.detail.mockReturnValue(
      throwError(() => new HttpErrorResponse({
        status: 500,
        error: { title: 'Error', detail: 'The district could not be loaded.' },
      })),
    );
    await selectFirstDistrict(fixture);

    const alert = el(fixture).querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('could not be loaded');
  });
});

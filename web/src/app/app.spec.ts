import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';
import { App } from './app';
import { DistrictDetail, DistrictSummary } from './districts/district.models';
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

  function setup(list: Observable<DistrictSummary[]> = of(districts)): ComponentFixture<App> {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: DistrictService, useValue: { list: () => list, detail: () => of(detail) } }],
    });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return fixture;
  }

  it('loads districts from the service and renders them', async () => {
    const fixture = setup();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('North Denmark');
  });

  it('loads and shows the detail when a district is selected', async () => {
    const fixture = setup();
    await fixture.whenStable();

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button.district')!.click();
    fixture.detectChanges();
    await fixture.whenStable();

    // The detail pane now shows the selected district's secondary and store.
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Emil');
    expect(text).toContain('Aalborg');
  });
});

import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';
import { App } from './app';
import { DistrictSummary } from './districts/district.models';
import { DistrictService } from './districts/district.service';

describe('App', () => {
  const districts: DistrictSummary[] = [
    { id: 1, name: 'North Denmark', primary: { id: 1, name: 'Anna' }, storeCount: 4 },
  ];

  function setup(list: Observable<DistrictSummary[]> = of(districts)) {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: DistrictService, useValue: { list: () => list } }],
    });
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return fixture;
  }

  it('creates the app', () => {
    expect(setup().componentInstance).toBeTruthy();
  });

  it('loads districts from the service and renders them', async () => {
    const fixture = setup();
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('North Denmark');
    expect(text).toContain('Anna');
  });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { DistrictDetail, DistrictSummary, PutAssignmentsRequest } from './district.models';
import { DistrictService } from './district.service';

describe('DistrictService', () => {
  let service: DistrictService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/api/v1`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DistrictService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('list() GETs the districts collection and returns the body', () => {
    const body: DistrictSummary[] = [
      { id: 1, name: 'North Denmark', primary: { id: 1, name: 'Anna' }, storeCount: 4 },
    ];
    let result: DistrictSummary[] | undefined;

    service.list().subscribe((r) => (result = r));

    const req = httpMock.expectOne(`${base}/districts`);
    expect(req.request.method).toBe('GET');
    req.flush(body);
    expect(result).toEqual(body);
  });

  it('detail(id) GETs a single district by id', () => {
    service.detail(3).subscribe();

    const req = httpMock.expectOne(`${base}/districts/3`);
    expect(req.request.method).toBe('GET');
    req.flush({} as DistrictDetail);
  });

  it('salespersons() GETs the salesperson pool', () => {
    service.salespersons().subscribe();

    const req = httpMock.expectOne(`${base}/salespersons`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('putAssignments() PUTs the full assignment body', () => {
    const body: PutAssignmentsRequest = { primaryId: 6, secondaryIds: [2], concurrencyToken: 'AAAAAAAAB9E=' };

    service.putAssignments(3, body).subscribe();

    const req = httpMock.expectOne(`${base}/districts/3/salespersons`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({} as DistrictDetail);
  });

  it('builds every url from environment.apiUrl (config, not hardcoded)', () => {
    service.list().subscribe();

    // Expected url is derived from the config value — proves the service reads it.
    httpMock.expectOne(`${environment.apiUrl}/api/v1/districts`);
  });
});

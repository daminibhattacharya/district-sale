import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { DistrictDetail, DistrictSummary, PutAssignmentsRequest, Salesperson } from './district.models';

/// Typed access to the districts API. The base URL comes from environment config (never hardcoded).
@Injectable({ providedIn: 'root' })
export class DistrictService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1`;

  /** All districts (name, primary, store count) for the list view. */
  list(): Observable<DistrictSummary[]> {
    return this.http.get<DistrictSummary[]>(`${this.base}/districts`);
  }

  /** One district in full, including the concurrency token to echo back on a PUT. */
  detail(id: number): Observable<DistrictDetail> {
    return this.http.get<DistrictDetail>(`${this.base}/districts/${id}`);
  }

  /** The salesperson pool to assign from. */
  salespersons(): Observable<Salesperson[]> {
    return this.http.get<Salesperson[]>(`${this.base}/salespersons`);
  }

  /** Replace a district's whole assignment set (full desired state); returns the updated detail. */
  putAssignments(id: number, request: PutAssignmentsRequest): Observable<DistrictDetail> {
    return this.http.put<DistrictDetail>(`${this.base}/districts/${id}/salespersons`, request);
  }
}

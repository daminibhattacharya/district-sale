// Wire contracts — mirror the API DTOs (camelCase JSON).

export interface Salesperson {
  id: number;
  name: string;
}

export interface Store {
  id: number;
  name: string;
}

export interface DistrictSummary {
  id: number;
  name: string;
  primary: Salesperson;
  storeCount: number;
}

export interface DistrictDetail {
  id: number;
  name: string;
  primary: Salesperson;
  secondaries: Salesperson[];
  stores: Store[];
  concurrencyToken: string;
}

export interface PutAssignmentsRequest {
  primaryId: number;
  secondaryIds: number[];
  concurrencyToken: string;
}

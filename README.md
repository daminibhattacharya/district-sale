# District / Salesperson Management

Salespersons cover sales districts; districts contain stores. This is a working end-to-end slice that
shows who covers what and lets a user change coverage — add or remove salespersons and change who is in
charge — without calling IT.

It is built **test-first**, bottom-up, with the business rules enforced **in the database** wherever
possible and a passing test at every layer.

**Stack:** SQL Server · ASP.NET Core + Dapper (native SQL, no ORM) · Angular (standalone) · xUnit + Moq ·
Vitest · Playwright · Serilog · GitHub Actions CI.

---

## Architecture

```
src/
  Domain/        Aggregates + invariants in plain C# (District, Salesperson, Store) — no dependencies
  DataAccess/    Dapper repositories + the versioned SQL scripts (Sql/001_schema.sql, 010_seed.sql)
  Api/           ASP.NET Core: controllers, DTOs, ProblemDetails, Serilog, health, DI
tests/
  Tests.Unit/         Fast domain + service unit tests (Moq) — no database
  Tests.Integration/  DB business-rule, data-layer and API contract tests — real SQL Server
web/             Angular client (typed HTTP service, container/presentational components)
e2e/             One Playwright end-to-end journey against the running stack
```

Layering runs inward: `Api → DataAccess → Domain`. The domain holds the rules that can live in C#; the
schema enforces the same rules as a backstop; the API maps typed exceptions to HTTP.

---

## How to run

### Prerequisites
- **.NET SDK 10**
- A **SQL Server** instance (Azure SQL or a local/container SQL Server)
- **Node 24** (or 22 LTS) + npm — for the Angular client and the E2E. *Node 23 is rejected by the Angular toolchain.*

### Configuration (no secrets in Git)
- **API** reads its connection string from `ConnectionStrings:Sql` — supply it via the environment
  variable `ConnectionStrings__Sql` or `dotnet user-secrets`.
- **Local convenience:** running the API in Development seeds an empty database on startup (idempotent).
  This is opt-in via `Seed:OnStartup` and already switched on in `launchSettings.json`, so `dotnet run`
  "just works" against a fresh database. It never runs in the test host.

### Run the API + client
```bash
# API — reads ConnectionStrings__Sql; seeds an empty DB on first run (Development)
export ConnectionStrings__Sql='Server=…;Database=…;User ID=…;Password=…;Encrypt=True;TrustServerCertificate=False;'
dotnet run --project src/Api            # http://localhost:5091

# Angular client (separate terminal)
cd web
npm install
npm start                                # http://localhost:4200 → calls the API (apiUrl from env config)
```
The client's API base URL comes from `web/src/environments/environment*.ts` (`apiUrl`) — dev points at the
API origin, prod is same-origin. Nothing is hardcoded in the components.

### Tests
```bash
dotnet test --filter Category=Unit          # fast, no database
dotnet test --filter Category=Integration   # needs DISTRICT_SQL_TEST (a throwaway test database)
cd web && npm test                          # Angular unit tests (Vitest + jsdom, headless)
cd e2e && npm install && npx playwright install chromium && npm test   # one E2E journey
```
Point `DISTRICT_SQL_TEST` at a **throwaway** database (the integration tier applies the schema and wipes
data between tests):
```bash
export DISTRICT_SQL_TEST='Server=…;Database=DistrictSalesTest;User ID=…;Password=…;Encrypt=True;TrustServerCertificate=False;'
```
When `DISTRICT_SQL_TEST` is unset, the integration tests **skip** (never silently pass) so a contributor
without database access still gets a green unit run.

---

## Testing

One working example of every required tier, all green in CI on each push:

| Tier | Where | Needs a DB |
|---|---|---|
| **Business-rule tests** (each §4 rule rejected by the DB) | `Tests.Integration` | yes |
| Data-layer integration (Dapper against real SQL) | `Tests.Integration` | yes |
| API unit tests (validation, mapping, error paths — Moq) | `Tests.Unit` | no |
| API contract tests (request → DB → response, `WebApplicationFactory`) | `Tests.Integration` | yes |
| Angular unit tests (service + components, incl. loading/empty/error) | `web` | no |
| One end-to-end (Playwright: pick → add → verify → remove → verify) | `e2e` | yes |

**CI** (`.github/workflows/ci.yml`) runs four jobs on every push and fails on any red:
`build-and-test` (unit + the **EF/LINQ-to-SQL ban gate**), `web` (Vitest), `integration`, and `e2e`.
The last two spin up an **ephemeral SQL Server container** on the runner — so the whole suite is green on a
clean clone with no external database (this is the "test system without external dependencies" from §8,
done rather than written up). Unit/integration results render as GitHub **Checks**; the Playwright HTML
report uploads as an artifact.

---

## Database & business rules

Rules come from the client and are enforced in the database wherever a constraint can express them.

| # | Rule | Enforcement |
|---|---|---|
| BR-1 | Every district has a name | `NOT NULL` + `CHECK (LEN(Name) > 0)` + `UNIQUE(Name)` |
| BR-2 | A store belongs to exactly one district | `DistrictId NOT NULL` FK, `ON DELETE CASCADE` |
| BR-3 | A salesperson is in ≥ 1 district | seed-guaranteed + documented + assertion test *(see below)* |
| **BR-4** | A district **always** has **exactly one** primary | `PrimarySalespersonId NOT NULL` FK → pure constraint |
| BR-5 | A district may have 0+ secondaries | link table (no lower bound) |
| BR-6 | The same salesperson may be primary of several districts | no `UNIQUE` on `PrimarySalespersonId` |
| BR-7 | Not both primary and secondary of the same district | composite PK (duplicate) + domain guard (cross-table) |

Every rule has a failing-then-passing integration test that proves the database rejects the violation
(or, where a constraint can't express it, a documented exception covered by a test).

---

## Decisions & trade-offs

- **BR-4 is a pure constraint.** The primary is modelled as a `NOT NULL` foreign-key **column** on
  `District`, not a "primary" row in a link table. That makes the headline rule impossible to violate by
  construction: a district cannot exist without a primary (`NOT NULL`), the primary must be a real
  salesperson (FK), and there is exactly one because it is a single column — no trigger needed. Secondary
  salespersons (0..n) live in a separate link table. Primary and secondary genuinely differ in cardinality
  (exactly-one vs zero-or-more), so they are modelled differently.

- **BR-7's cross-table half is enforced in the domain, not the DB.** "Not both primary *and* secondary of
  the same district" compares `District.PrimarySalespersonId` against a link-table row — a `CHECK` cannot
  reference another table. It is enforced in the `District` aggregate (adding a secondary rejects the
  current primary; promoting a secondary drops the secondary row in the same write) and covered by unit
  tests. The in-district *duplicate*-secondary half is a pure constraint (composite PK).

- **BR-3 (every salesperson belongs to ≥1 district) is seed-guaranteed, not constraint-enforced.** A
  salesperson is "in" a district by being its primary (a column on `District`) or via a
  `DistrictSecondarySalesperson` row. "Referenced by at least one of two other tables" is not something a
  column constraint can express, and a trigger enforcing it would block the legitimate intermediate state
  of creating a salesperson before assigning them. The rule is upheld by the seed and pinned by an
  assertion test (`SeedDataTests`).

- **PUT-vs-DELETE (the Commandments conflict).** The Commandments allow **GET/POST/PUT only**, but the
  client needs removal. Resolution: `PUT /api/v1/districts/{id}/salespersons` replaces the **full**
  assignment set — sending a shorter list *is* the removal. One idempotent contract, no `DELETE`. Removing
  the last primary is impossible by construction (a valid PUT always names a primary), and the API rejects
  a body that would leave none.

- **Optimistic concurrency.** `District` carries a `rowversion`; the PUT echoes it back and the update runs
  `WHERE RowVersion = @token`, so two editors can't silently overwrite each other — a stale token → **409**.

- **Seed is applied on demand, not at test startup.** The integration harness resets every table between
  tests (Respawn), so seed loaded at startup would be wiped before the first test. `010_seed.sql` is kept
  out of the schema scripts and applied explicitly by the tests that need it; it is re-runnable.

- **All datetimes are UTC.** The only datetime on the wire today is the `/health` timestamp
  (`DateTime.UtcNow`, serialized with a trailing `Z`, asserted by a contract test). Any future timestamp
  follows the same rule.

- **Angular on Node 24.** The system default (Node 23) is rejected by the Angular toolchain; the client and
  E2E use Node 24 (22 LTS also works).

---

## API

Versioned under `/api/v1`, JSON, UTC:

| Need | Endpoint |
|---|---|
| List all districts (name, primary, store count) | `GET /api/v1/districts` |
| District detail (primary, secondaries, stores, concurrency token) | `GET /api/v1/districts/{id}` |
| List salespersons (picker) | `GET /api/v1/salespersons` |
| Change a district's assignments (full set) | `PUT /api/v1/districts/{id}/salespersons` |
| Liveness | `GET /health` |

Errors are **RFC 9457 ProblemDetails** with a `traceId`; the client displays the server's message. Typed
domain exceptions map to status codes (not found → 404, conflict/concurrency → 409). Every response carries
a **correlation id** (Serilog enriched), and unhandled errors never leak internals.

---

## Production readiness

Minimum viable is the bar. Everything is either **built once** or **written up** with the design we'd use.

**Built (small and real):** Serilog structured logging with correlation ids · DI container · global
exception handling → ProblemDetails · config with no secrets in Git · `/health` endpoint · versioned,
re-runnable migration + seed scripts · one CI pipeline running the whole pyramid · an ephemeral,
dependency-free test database in CI.

**Write it up (design + why we stopped):**

- **Windows/AD authentication.** Anonymous in dev behind a pluggable auth seam. In production we'd add
  Negotiate/Windows auth for on-prem, or Microsoft Entra ID (JWT bearer) for cloud, wired at the
  composition root; authorization policies gate the mutation endpoint. Stopped at the seam because there is
  no AD instance to target here.
- **Log shipping to Kibana.** Logs are already structured JSON with correlation ids. We'd add a Serilog
  Elasticsearch/OTLP sink (or write to stdout and let Filebeat/Fluent Bit forward) into ELK, and build
  dashboards keyed on correlation id and status.
- **Coverage-change events (RabbitMQ / SignalR).** A successful assignment change would publish a
  `DistrictAssignmentsChanged` event — to **RabbitMQ** for other systems (via an outbox so the DB write and
  the publish can't diverge), and/or push over **SignalR** to connected clients for live updates.
- **Failover / HA.** Azure SQL with a failover group (or an AG on-prem); a stateless API scaled to multiple
  instances behind a load balancer with health-check-based routing.
- **Deploy & rollback.** CI produces the build artifact; deploy to a staging slot, gate on `/health`, then
  blue-green swap. Schema changes stay forward-only and backward-compatible so a rollback is a slot swap
  back with no data migration. Rollback is documented alongside each release.
- **Alerting thresholds.** Alert on 5xx rate, p95 latency, DB connection failures, and health-check
  failures, off the same log/metrics pipeline.
- **Data Portal registration.** Register the API in the organisation's data catalogue with ownership,
  schema, and SLA metadata.

---

## Definition of done

- [x] Schema scripts in Git; every §4 rule enforced by a constraint **or** documented + test-covered
- [x] Seed data covers the §6.2 edge cases and is re-runnable
- [x] API complete: JSON, UTC, versioned, ProblemDetails errors, optimistic concurrency, health endpoint
- [x] **No EF or LINQ-to-SQL anywhere** — verified by a CI check, not by eyeballing
- [x] Angular client complete; loading / empty / error states handled; keyboard accessible
- [x] At least one passing test in every tier; business rules at full coverage
- [x] CI green on a clean clone
- [x] README covers how to run, schema decisions & trade-offs, the PUT-vs-DELETE conflict, and the §8 gaps
- [x] Everything committed to Git, SQL included

# District / Salesperson Management

Shows which salespersons cover which sales districts (and the stores in them), and lets a
user change that coverage — add or remove salespersons and change who is in charge —
without calling IT.

Built **test-first** (bottom-up), with the business rules enforced in the database wherever
possible and a passing test at every layer.

**Stack:** SQL Server · ASP.NET Core + Dapper (native SQL, no ORM) · Angular · xUnit + Moq ·
Serilog · GitHub Actions CI.

## Status

🚧 Under construction — see the build progresses as a series of atomic commits.

## How to run

> Filled in as the pieces land.

### Prerequisites
- .NET SDK 10
- A SQL Server instance (connection string supplied via config — see below)
- Node 22 LTS + npm (for the Angular client)

### Configuration
- **API:** connection string via `ConnectionStrings__Sql` (environment variable or user-secrets — no secrets in Git).
- **Integration tests:** point `DISTRICT_SQL_TEST` at a throwaway test database.

### Build & test
```bash
dotnet build
dotnet test --filter Category=Unit          # fast, no database
dotnet test --filter Category=Integration   # requires DISTRICT_SQL_TEST
```

## Decisions & trade-offs

*A living record of choices made and why — updated as the build proceeds.*

- **BR-3 (every salesperson belongs to ≥1 district) is seed-guaranteed, not constraint-enforced.**
  A salesperson is "in" a district by being its primary (a column on `District`) or by a row in
  `DistrictSecondarySalesperson`. "Referenced by at least one of two other tables" is not something a
  column constraint can express, and a trigger enforcing it would block the legitimate intermediate
  state of creating a salesperson before assigning them. So the rule is upheld by the seed data and
  pinned by an assertion test (`SeedDataTests`), rather than by the schema.
- **Seed is applied on demand, not at startup.** The integration harness resets every table between
  tests (Respawn), so seed loaded at startup would be wiped before the first test. `010_seed.sql` is
  therefore kept out of the schema scripts and applied explicitly (`ApplySeedAsync`) by the tests that
  need it. The script is re-runnable (clears then re-inserts) so repeated application is a no-op.
- **All datetimes are UTC.** The API exposes no local times. The only datetime on the wire today is
  the `/health` timestamp; it is `DateTime.UtcNow` and serialized with a trailing `Z`, asserted by a
  contract test. Any future timestamp follows the same rule.

## Definition of done

- [ ] Schema scripts in Git; every business rule enforced by a constraint or documented + test-covered
- [ ] Seed data covers the edge cases and is re-runnable
- [ ] API complete: JSON, UTC, versioned, ProblemDetails errors, optimistic concurrency, health endpoint
- [ ] No EF or LINQ-to-SQL anywhere — verified by a CI check
- [ ] Angular client complete; loading / empty / error states handled
- [ ] At least one passing test in every tier; business rules at full coverage
- [ ] CI green on a clean clone

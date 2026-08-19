# StudyPilot (SP)

AI academic productivity assistant. See `StudyPilot_SOW.docx` for the approved scope baseline.

## Architecture

Modular monolith (ADR-001) on ASP.NET Core (ADR-002).

```
src/
  StudyPilot.Api/              Composition root — the only project that knows the module set
  StudyPilot.SharedKernel/     Domain primitives. Depends on nothing.
  StudyPilot.Infrastructure/   Cross-cutting technical concerns. No domain logic.
  Modules/
    StudyPilot.Modules.Identity/   Users, auth, profiles, preferences   (Epic SP-1)
    StudyPilot.Modules.Academic/   Universities, periods, courses       (Epic SP-2)
tests/
  StudyPilot.Architecture.Tests/     Enforces the boundary rules below
  StudyPilot.Modules.Identity.Tests/
```

### Boundary rules

These are asserted by `StudyPilot.Architecture.Tests`, not left to code review:

- Modules never reference one another — they communicate through the host.
- Shared infrastructure never references a module.
- The shared kernel depends on neither infrastructure nor modules.
- Shared infrastructure declares no domain entities or aggregate roots.
- Every module type is sealed and constructible without a container.

Adding a module means implementing `IModule` and registering it in `Program.cs`. The
`/modules` endpoint reports what the running host actually composed.

## Persistence

PostgreSQL is the system of record (ADR-006). Each module owns its own schema — `identity`,
`academic` — so module boundaries hold in the database as well as in code, and each module's
migration history lives inside its own schema.

Aggregate roots automatically get PostgreSQL's `xmin` as an optimistic-concurrency token, so a
concurrent write fails loudly instead of silently discarding the earlier one. Entities
implementing `IAuditable` are timestamped by the persistence layer, and `CreatedAtUtc` cannot be
rewritten after insert.

Migrations are **not** applied on startup by default (`Database:MigrateOnStartup`). Schema
changes should be a deliberate step rather than a side effect of a deploy.

## Running

Start the local database first:

```bash
docker compose up -d
```

Then the API:

```bash
dotnet run --project src/StudyPilot.Api
```

| Endpoint | Purpose |
|---|---|
| `/health/live` | Liveness — runs no checks; a database outage must not trigger a restart |
| `/health/ready` | Readiness — includes every module's database check |
| `/modules` | Composed module set |
| `/openapi/v1.json` | OpenAPI document (development only) |

## Testing

```bash
dotnet test StudyPilot.slnx
```

Persistence tests run against a real PostgreSQL container via Testcontainers, so Docker must be
running. The infrastructure under test is PostgreSQL-specific — schemas, `xmin` concurrency — and
an in-memory provider would not exercise it.

## Jira workflow

Tooling in `scripts/` talks to Jira via the REST API; credentials live in a gitignored
`.env` (see `.env.example`).

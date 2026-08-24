# Warden

A template for a .NET 10 + Nuxt 4 admin application with RBAC (roles + granular permissions,
including wildcards like `user.*` and `*`), a JWT auth flow, and an Orchard-like admin UI that
lets you manage users and roles/permissions at runtime.

## Stack

- **Backend**: ASP.NET Core 10 Web API in an MVCS shape (Model / Controller / Service — no
  Repository layer, Services use EF Core's `DbContext` directly), EF Core 10 with swappable
  SQLite / PostgreSQL providers, JWT bearer auth (access + refresh token).
- **Frontend**: Nuxt 4 + `@nuxt/ui` v4 + Pinia.

## Project layout

```
src/
  Warden.Domain/         entities, PermissionConstants — no framework dependencies
  Warden.Application/    services, DTOs, permission/auth plumbing
  Warden.Infrastructure/ EF Core DbContexts (per provider), migrations, seed data
  Warden.WebApi/         Program.cs, controllers, appsettings.json
frontend/                Nuxt 4 admin app
```

## Using this as a template for a new project

This repo is itself a [`dotnet new` template](https://learn.microsoft.com/dotnet/core/tools/custom-templates)
— generating from it renames every `Warden`/`warden` occurrence (namespaces, project/solution
files, seed email domain, JWT issuer/audience, cookie name, docker-compose service names, page
titles) to whatever name you give it, and rebuilds cleanly with zero manual edits.

Install it once (from this repo's root):

```
dotnet new install .
```

Then generate a new project from it, anywhere:

```
dotnet new warden -n AcmeCrm -o ../AcmeCrm
```

This produces `AcmeCrm.sln`, `src/AcmeCrm.Domain`, `src/AcmeCrm.WebApi`, an `AcmeCrm.Client` JWT
audience, a `acmecrm.db` connection string, an `admin@acmecrm.local` seed user, an
`acmecrm_refresh_token` cookie, etc. — all consistently renamed. Run `dotnet build` and
`pnpm install` in the output as usual to confirm it builds (it will).

To pick up changes made to this template later, reinstall with `dotnet new install . --force`.
To remove it, `dotnet new uninstall .` (run from this repo's root).

## Prerequisites

- .NET 10 SDK
- Node.js + [pnpm](https://pnpm.io)
- Optionally Docker, if you want to run PostgreSQL via `docker-compose.yml` instead of SQLite

## Running the backend

```
cd src/Warden.WebApi
dotnet run
```

On first run this applies EF Core migrations and seeds:

- A `SuperAdmin` role with the `*` wildcard permission
- An admin user (`admin@warden.local` / `ChangeMe123!` by default — override via the
  `SeedAdmin` section in `appsettings.json` or `SeedAdmin__*` env vars before first run)

The API listens on `http://localhost:5083` and serves Scalar UI at `/scalar` in Development.

### Switching database provider

Set `Database:Provider` in `appsettings.json` (or `Database__Provider` env var) to `Sqlite` or
`Postgres`, and update `ConnectionStrings:Default` to match. To run Postgres locally:

```
docker compose up -d postgres
```

then set:

```json
"Database": { "Provider": "Postgres" },
"ConnectionStrings": { "Default": "Host=localhost;Port=5432;Database=warden;Username=postgres;Password=postgres" }
```

### Migrations

Each provider owns its own migrations folder (`Persistence/Migrations/Sqlite`,
`Persistence/Migrations/Postgres`) because migrations are provider-specific SQL. To add a new
migration after changing an entity, run it once per provider:

```
dotnet ef migrations add YourMigrationName --context SqliteAppDbContext -o Persistence/Migrations/Sqlite --project src/Warden.Infrastructure
dotnet ef migrations add YourMigrationName --context PostgresAppDbContext -o Persistence/Migrations/Postgres --project src/Warden.Infrastructure
```

Adding a third provider (e.g. MariaDB, once its EF Core provider catches up to EF Core 10) means
adding one more thin `AppDbContext` subclass + design-time factory + migrations folder — the
shared model lives entirely in the abstract `AppDbContext` base class.

## Adding a new permission

1. Add a constant to `Warden.Domain/Permissions/PermissionConstants.cs`, e.g. `Reports.Export = "report.export"`.
2. Decorate the controller action with `[RequirePermission(PermissionConstants.Reports.Export)]`.

That's it — the permission catalog (`GET /api/admin/permissions/catalog`) discovers it via
reflection, so it automatically appears in the admin Roles page's permission matrix with no
other code changes. Granting it to a role takes effect immediately for any already-logged-in
user (no re-login required) because permission lookups are cached for 60 seconds and the cache
is invalidated whenever a role's permissions are edited.

## Running the frontend

```
cd frontend
pnpm install
pnpm run dev
```

Runs on `http://localhost:3000`. Set `NUXT_PUBLIC_API_BASE` (see `.env.example`) if the backend
isn't on `http://localhost:5083`.

## Auth model

Login issues a short-lived JWT access token plus an opaque refresh token. The frontend keeps the
access token in memory and the refresh token in a cookie; a 401 from any API call triggers a
silent refresh-and-retry once before falling back to the login page.

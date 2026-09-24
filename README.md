# Warden

English | [繁體中文](README.tw.md)

A template for a .NET 10 + Nuxt 4 admin application with RBAC (roles + granular permissions,
including wildcards like `user.*` and `*`), OIDC login (e.g. Google) backed by the app's own JWT
session, user-owned API keys with restrictable permission scopes, and an Orchard-like admin UI
that lets you manage users, roles/permissions and API keys at runtime.

## Stack

- **Backend**: ASP.NET Core 10 Web API in an MVCS shape (Model / Controller / Service — no
  Repository layer, Services use EF Core's `DbContext` directly), EF Core 10 with swappable
  SQLite / PostgreSQL / MariaDB providers, OIDC login + JWT bearer auth (access + refresh token),
  plus API key (personal access token) auth on the same `Bearer` header.
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
- An OIDC provider app registration (e.g. a Google OAuth client) — see below
- Optionally Docker, if you want to run PostgreSQL or MariaDB via `docker-compose.yml` instead of SQLite

## Running the backend

```
cd src/Warden.WebApi
dotnet run
```

On first run this applies EF Core migrations and seeds:

- A `SuperAdmin` role with the `*` wildcard permission
- A placeholder admin account for `SeedAdmin:Email` (`admin@warden.local` by default — override
  via the `SeedAdmin` section in `appsettings.json` or `SeedAdmin__*` env vars). It has no
  password: the first person to log in through a configured OIDC provider with that email
  inherits `SuperAdmin`.

The API listens on `http://localhost:5083` and serves Scalar UI at `/scalar` in Development.

### Configuring an OIDC provider

There is no local password login — configure at least one OIDC provider under `Oidc:Providers`
(in `appsettings.json`, user-secrets, or env vars / the root `.env`, see `.env.example`).
For example, Google:

```
Oidc__FrontendBaseUrl=http://localhost:3000
Oidc__Providers__0__Name=google
Oidc__Providers__0__DisplayName=Google
Oidc__Providers__0__Authority=https://accounts.google.com
Oidc__Providers__0__ClientId=<client-id>
Oidc__Providers__0__ClientSecret=<client-secret>
```

Register `{ApiBase}/api/auth/callback/{Name}` (e.g. `http://localhost:5083/api/auth/callback/google`)
as an authorized redirect URI in that provider's console. Each provider gets its own
"Sign in with …" button on the login page. `Scope` defaults to `openid profile email`.

### Switching database provider

Set `Database:Provider` in `appsettings.json` (or `Database__Provider` env var) to `Sqlite`,
`Postgres`, or `MariaDb`, and update `ConnectionStrings:Default` to match. To run Postgres or
MariaDB locally:

```
docker compose up -d postgres
docker compose up -d mariadb
```

then set:

```json
"Database": { "Provider": "Postgres" },
"ConnectionStrings": { "Default": "Host=localhost;Port=5432;Database=warden;Username=postgres;Password=postgres" }
```

```json
"Database": { "Provider": "MariaDb" },
"ConnectionStrings": { "Default": "Server=localhost;Port=3306;Database=warden;User=root;Password=root;" }
```

### Migrations

Each provider owns its own migrations folder (`Persistence/Migrations/Sqlite`,
`Persistence/Migrations/Postgres`, `Persistence/Migrations/MariaDb`) because migrations are
provider-specific SQL. To add a new migration after changing an entity, run it once per provider:

```
dotnet ef migrations add YourMigrationName --context SqliteAppDbContext -o Persistence/Migrations/Sqlite --project src/Warden.Infrastructure
dotnet ef migrations add YourMigrationName --context PostgresAppDbContext -o Persistence/Migrations/Postgres --project src/Warden.Infrastructure
dotnet ef migrations add YourMigrationName --context MariaDbAppDbContext -o Persistence/Migrations/MariaDb --project src/Warden.Infrastructure
```

Adding another provider means adding one more thin `AppDbContext` subclass + design-time factory +
migrations folder — the shared model lives entirely in the abstract `AppDbContext` base class.

## Adding a new permission

1. Add a constant to `Warden.Domain/Permissions/PermissionConstants.cs`, e.g. `Reports.Export = "report.export"`.
2. Decorate the controller action with `[RequirePermission(PermissionConstants.Reports.Export)]`.

That's it — the permission catalog (`GET /api/admin/permissions/catalog`) discovers it via
reflection, so it automatically appears in the admin Roles page's permission matrix (and as a
selectable API key scope) with no other code changes. Granting it to a role takes effect
immediately for any already-logged-in user (no re-login required) because permission lookups
are cached for 60 seconds and the cache is invalidated whenever a role's permissions are edited.

## Running the frontend

```
cd frontend
pnpm install
pnpm run dev
```

Runs on `http://localhost:3000`. Set `NUXT_PUBLIC_API_BASE` (see `.env.example`) if the backend
isn't on `http://localhost:5083`.

## Auth model

Login goes through an OIDC provider: the backend handles the provider callback, provisions (or
links, by email) the user, and redirects to the frontend's `/auth/callback` with a single-use
handoff code. The frontend exchanges that code (`POST /api/auth/oidc/exchange`) for a short-lived
JWT access token plus an opaque refresh token. It keeps the access token in memory and the
refresh token in a `SameSite=Lax` cookie (Lax, so the cookie is still sent on the cross-site
redirect chain back from the provider and SSR sees the session); a 401 from any API call
triggers a silent refresh-and-retry once before falling back to the login page.

## API keys

Users can create personal API keys (personal access tokens) for scripts and integrations on the
admin UI's **API Keys** page (`/admin/api-keys`). Every user manages only their own keys — this
is a self-service feature, not an admin view over other users' keys.

- **Format**: `wdn_<12-char key id>_<secret>`. The raw key is shown **once**, at creation. Only a
  SHA-256 hash of the secret is stored; afterwards the UI shows just the last 4 characters.
- **Usage**: send it exactly like a JWT access token:

  ```
  curl -H "Authorization: Bearer wdn_0123456789ab_..." http://localhost:5083/api/admin/users
  ```

  `Bearer` is a policy scheme that forwards to the API key handler when the token starts with
  `wdn_` and to the JWT handler otherwise, so `[Authorize]` / `[RequirePermission]` work
  unchanged for both.
- **Scopes**: each key has one or more permission scopes — exact keys like `user.read`, module
  wildcards like `user.*`, or `*`. A key can only **narrow** its owner's permissions, never widen
  them: you can't grant a scope you don't have at creation, and on every request the owner's
  *current* role permissions are intersected with the key's scopes. Removing a permission from
  the owner's role therefore also takes it away from all of their keys.
- **Lifecycle**: optional expiry (Never / 30 days / 90 days / 1 year in the UI), revocable at any
  time, and deletable. Revoking keeps the key listed (marked Revoked) for auditing; deleting
  removes it and its scopes permanently. Revoked, expired or deleted keys, and keys whose owner
  is disabled, get a 401. `LastUsedAtUtc` is
  updated at most once a minute per key.
- **Permissions**: the feature itself is gated by `apikey.read`, `apikey.create`, `apikey.revoke`
  and `apikey.delete` (or `apikey.*` for all of them).

| Method | Path | Permission | Description |
|---|---|---|---|
| `GET` | `/api/admin/api-keys` | `apikey.read` | List your own keys |
| `POST` | `/api/admin/api-keys` | `apikey.create` | Create a key — body `{ "name", "expiresAtUtc", "scopes": [...] }`; the response contains the raw key (only time it's returned) |
| `POST` | `/api/admin/api-keys/{id}/revoke` | `apikey.revoke` | Revoke one of your keys (kept for auditing) |
| `DELETE` | `/api/admin/api-keys/{id}` | `apikey.delete` | Permanently delete one of your keys |

The `wdn_` prefix is **not** renamed by `dotnet new warden`; change `ApiKeyFormat.Prefix` in
`Warden.Application/Services/ApiKeys/ApiKeyFormat.cs` if you want a project-specific one.

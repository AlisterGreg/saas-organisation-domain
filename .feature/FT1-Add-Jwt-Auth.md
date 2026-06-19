# FT1 — JWT Authentication via Organisation-scoped API Keys

## Summary

Add authentication to the API. Each **Organisation** is issued one or more **API
keys** (seeded, not self-service). A client exchanges an API key for a short-lived
**JWT access token** plus a **refresh token**. The JWT is required on the existing
Organisation endpoints, and the token's organisation claim must match the
organisation being accessed (per-tenant isolation).

Persistence uses **SQLite via EF Core (code-first with migrations)**. API keys and
refresh tokens are never stored in plaintext — only their **SHA-256 hashes** are
persisted.

## Goals

- Issue and validate JWTs signed with **HMAC-SHA256** (symmetric secret from config).
- Exchange a seeded, organisation-scoped API key for an access + refresh token pair.
- Refresh access tokens via a rotating refresh-token flow with reuse detection.
- Protect the Organisation endpoints with `[Authorize]` and enforce per-tenant access.

## Non-goals (out of scope for FT1)

- Self-service user/organisation registration or an admin key-minting endpoint.
- Roles, scopes, or fine-grained permissions beyond the organisation claim.
- API-key rotation/revocation endpoints (revocation is supported in the data model
  and via reuse detection, but no management API is exposed yet).
- Password / interactive login. The API key is the only credential.
- Persisting the `Organisation` aggregate itself — the existing services remain
  stubbed; only the auth tables are persisted.

## Decisions (locked)

| Area | Decision |
|------|----------|
| Key issuance | **Seeded** in the database on startup. No issuance endpoint. |
| Principal | API key belongs to an **Organisation** (tenant). JWT carries an `org` claim. |
| Token model | **Access JWT + refresh token** (refresh persisted in SQLite). |
| Endpoint protection | Org endpoints require a valid JWT. Auth endpoints are anonymous. |
| Persistence | **EF Core + SQLite**, code-first with **migrations** checked in. |
| Secret storage | API keys and refresh tokens stored as **SHA-256 hashes** only. |
| JWT signing | **HMAC-SHA256** (`HS256`); secret from config / user-secrets / env. |
| Tenant scoping | `org` claim **must match** the route `{reference}` → `403` on mismatch. |
| Access token lifetime | **15 minutes**. |
| Refresh token lifetime | **7 days**, single-use (**rotated** on each refresh). |
| Refresh reuse | Presenting an already-rotated/revoked refresh token → `401` and the token's chain is revoked. |
| API key transport | `X-Api-Key: <key>` request header on `POST /api/auth/token`. |

## NuGet packages to add

- `Microsoft.AspNetCore.Authentication.JwtBearer` (net10.0)
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Design` (design-time, for migrations)

## Data model (EF Core / SQLite)

Database file (dev): `Data Source=auth.db` via `ConnectionStrings:AuthDb`.

### `ApiKey`
| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` (PK) | |
| `OrganisationReference` | `string` | e.g. `ORG-001`; this becomes the JWT `org` claim. |
| `KeyHash` | `string` | SHA-256 (hex/base64) of the raw key. **Unique index.** |
| `Label` | `string?` | Human-readable name for the key. |
| `CreatedAt` | `DateTimeOffset` | |
| `RevokedAt` | `DateTimeOffset?` | Non-null ⇒ key rejected. |

### `RefreshToken`
| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` (PK) | |
| `TokenHash` | `string` | SHA-256 of the raw refresh token. **Unique index.** |
| `ApiKeyId` | `Guid` (FK → ApiKey) | Owning key/organisation. |
| `OrganisationReference` | `string` | Denormalised for fast claim issuance. |
| `ExpiresAt` | `DateTimeOffset` | now + 7 days. |
| `CreatedAt` | `DateTimeOffset` | |
| `RevokedAt` | `DateTimeOffset?` | Set on rotation, logout, or reuse detection. |
| `ReplacedByTokenHash` | `string?` | Rotation chain link; enables reuse detection. |

> **Hashing note:** API keys and refresh tokens are high-entropy random secrets,
> so an unsalted SHA-256 is sufficient (no per-record salt / KDF needed). Lookups
> are by hash, never by plaintext.

## Project structure (follows existing feature-folder convention)

```
src/Saas.OrganisationDomain.Api/
  Auth/
    AuthController.cs                 # POST /api/auth/token, POST /api/auth/refresh
    Domain/
      ApiKey.cs                       # EF entity
      RefreshToken.cs                 # EF entity
      TokenResponse.cs                # record returned to clients
      RefreshRequest.cs               # record { string RefreshToken }
    Services/
      ApiKeyValidator.cs              # hash + look up X-Api-Key, check not revoked
      JwtTokenService.cs              # build/sign access JWT
      RefreshTokenService.cs          # issue / validate / rotate refresh tokens
  Persistence/
    AuthDbContext.cs                  # DbSet<ApiKey>, DbSet<RefreshToken>
    AuthDbSeeder.cs                   # seeds dev org + api key if empty
    Migrations/                       # checked-in EF migrations
  Controllers/Organisation/...        # existing — gains [Authorize] + tenant check
```

Services are registered `AddScoped<>` in `Program.cs`, matching the existing
`GetOrganisationService` / `CreateOrganisationService` pattern and primary-constructor
DI used by `OrganisationsController`.

## Configuration

`appsettings.json` (non-secret defaults) / `appsettings.Development.json` /
user-secrets (the `Jwt:Secret`):

```jsonc
{
  "ConnectionStrings": {
    "AuthDb": "Data Source=auth.db"
  },
  "Jwt": {
    "Issuer": "saas-organisation-domain",
    "Audience": "saas-organisation-domain",
    "Secret": "<dev-only secret here; real secret via user-secrets/env>",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

## JWT contents

Signed `HS256`. Claims:

- `iss` = `Jwt:Issuer`, `aud` = `Jwt:Audience`
- `sub` = `ApiKey.Id`
- `org` = `ApiKey.OrganisationReference`
- `jti` = random id, `iat`, `exp` (now + 15 min)

Validation (`TokenValidationParameters`): validate issuer, audience, lifetime, and
signing key; small `ClockSkew` (e.g. 30s).

## Endpoints

### `POST /api/auth/token` — exchange API key for tokens (anonymous)
- Request header: `X-Api-Key: <raw key>`
- Hash the key, look up a non-revoked `ApiKey` by `KeyHash`.
- On success: create access JWT + a new refresh token (random 256-bit, Base64Url),
  persist the refresh token hash, return:
  ```json
  {
    "accessToken": "<jwt>",
    "expiresIn": 900,
    "refreshToken": "<opaque>",
    "refreshExpiresIn": 604800,
    "tokenType": "Bearer"
  }
  ```
- Missing/invalid/revoked key ⇒ `401 Unauthorized`.

### `POST /api/auth/refresh` — rotate tokens (anonymous)
- Body: `{ "refreshToken": "<opaque>" }`
- Hash and look up the refresh token.
  - Not found / expired ⇒ `401`.
  - Already revoked (reuse) ⇒ revoke the rest of its rotation chain for that
    `ApiKeyId`, return `401`.
  - Owning `ApiKey` revoked ⇒ `401`.
- On success: revoke the presented token (set `RevokedAt`, `ReplacedByTokenHash`),
  issue a fresh access + refresh pair, return the same shape as `/token`.

### Existing Organisation endpoints — now protected
- `OrganisationsController` gains `[Authorize]` (auth endpoints stay
  `[AllowAnonymous]`).
- `GET /api/organisation/{reference}` and `POST /api/organisation`:
  compare the JWT `org` claim to the target organisation reference
  (`{reference}` for GET; `organisation.Reference` for POST). Mismatch ⇒
  `403 Forbidden`. Implemented as a small reusable guard (claims check helper or
  authorization handler).

## Program.cs wiring

1. Register `AuthDbContext` with `UseSqlite(connectionString)`.
2. Register `AddScoped` auth services.
3. `AddAuthentication().AddJwtBearer(...)` configured from `Jwt:*`.
4. `AddAuthorization()`.
5. On startup (dev): `db.Database.Migrate()` then run `AuthDbSeeder`.
6. Middleware order: `UseAuthentication()` → `UseAuthorization()` before
   `MapControllers()`.
7. (Optional) Add a Bearer security scheme to the OpenAPI document so tokens can be
   sent from the API explorer.

## Seed data (dev)

If `ApiKey` is empty, seed one organisation key:
- `OrganisationReference = "ORG-001"`
- A known dev key (e.g. `sk_test_dev_organisation_001`) — store only its SHA-256
  hash; document the plaintext in the README / `.http` file for local testing.

## Acceptance criteria

1. `POST /api/auth/token` with the seeded `X-Api-Key` returns `200` with a valid
   `accessToken` (15-min expiry), a `refreshToken`, and matching `expiresIn`.
2. An unknown/revoked `X-Api-Key` returns `401`.
3. `GET /api/organisation/ORG-001` returns `401` without a Bearer token and `200`
   with a valid token whose `org = ORG-001`.
4. A token for `ORG-001` requesting `GET /api/organisation/ORG-999` returns `403`.
5. `POST /api/organisation` with a body whose `reference` differs from the token's
   `org` claim returns `403`.
6. `POST /api/auth/refresh` with a valid refresh token returns a new pair and
   invalidates the old refresh token; reusing the old token returns `401` and
   revokes the chain.
7. Expired refresh tokens return `401`.
8. EF migrations are checked in; a fresh checkout produces `auth.db` on first run.
9. API keys and refresh tokens never appear in plaintext in the database.

## Manual test flow (update `Saas.OrganisationDomain.Api.http`)

```
### 1. Get tokens
POST {{host}}/api/auth/token
X-Api-Key: sk_test_dev_organisation_001

### 2. Call org endpoint with the access token
GET {{host}}/api/organisation/ORG-001
Authorization: Bearer {{accessToken}}

### 3. Refresh
POST {{host}}/api/auth/refresh
Content-Type: application/json

{ "refreshToken": "{{refreshToken}}" }
```

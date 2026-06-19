# saas-organisation-domain

Generic SaaS application exposing workspace, organisation, and user functionality.

## Authentication

The API is protected with JWT bearer authentication. Each organisation (tenant)
is issued one or more **API keys** (seeded, not self-service). A client exchanges
an API key for a short-lived **access token** plus a rotating **refresh token**.

- **Access token** — HMAC-SHA256 (`HS256`) JWT, 15-minute lifetime, carries an
  `org` claim identifying the tenant.
- **Refresh token** — opaque 256-bit secret, 7-day lifetime, single-use (rotated
  on every refresh, with reuse detection that revokes the rotation chain).
- API keys and refresh tokens are stored only as **SHA-256 hashes**, never in
  plaintext.

### Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| `POST` | `/api/auth/token` | `X-Api-Key` header | Exchange an API key for a token pair. |
| `POST` | `/api/auth/refresh` | refresh token in body | Rotate the token pair. |
| `GET`  | `/api/organisation/{reference}` | `Bearer` token | Read an organisation (org claim must match `{reference}`). |
| `POST` | `/api/organisation` | `Bearer` token | Create an organisation (org claim must match `reference` in the body). |

A token issued for one organisation cannot access another's resources — a
mismatch returns `403`.

### Local development

Persistence uses **SQLite via EF Core** (`auth.db`, created automatically on
first run in Development from the checked-in migrations). On startup the database
is migrated and, if empty, seeded with a single dev API key:

| Field | Value |
|-------|-------|
| Organisation | `ORG-001` |
| Dev API key (plaintext) | `sk_test_dev_organisation_001` |

> The plaintext key is for local testing only; only its SHA-256 hash is stored.

The JWT signing secret is read from `Jwt:Secret`. A throwaway value is set in
`appsettings.Development.json`; provide a real secret via user-secrets or an
environment variable for anything beyond local dev:

```bash
dotnet user-secrets set "Jwt:Secret" "<a long, random, 256-bit+ secret>"
```

Import [`Saas.OrganisationDomain.postman_collection.json`](Saas.OrganisationDomain.postman_collection.json)
into Postman for an end-to-end flow (get token → call endpoint → refresh, including
the cross-tenant `403` and refresh-reuse `401` cases). Send **Auth / 1. Get tokens**
first — its test script stores the access and refresh tokens as collection variables
that the other requests reuse. You can also run the whole collection headlessly:

```bash
npx newman run Saas.OrganisationDomain.postman_collection.json
```

The collection's `baseUrl` defaults to `http://localhost:5119`; override it (and
`apiKey`) as a collection variable or Postman environment if needed.

### Database migrations

EF migrations are checked in under
`src/Saas.OrganisationDomain.Api/Persistence/Migrations`. To add one after
changing the auth entities:

```bash
dotnet ef migrations add <Name> --context AuthDbContext --output-dir Persistence/Migrations
```

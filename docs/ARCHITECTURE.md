# Architecture

## 1. System Overview

This is a local MVP ASP.NET Core API for a logistics QR code workflow. The system manages packages, generates QR records for packages, resolves QR tokens anonymously, records scan attempts, and supports local JWT-based admin operations.

The current runtime is a single ASP.NET Core application using EF Core with SQLite. The frontend is expected to call the API directly. There is no BFF, queue, worker service, or external identity provider in this phase.

Primary flows:

1. Admin registers or logs in.
2. Admin creates a package.
3. Admin creates a QR code for the package.
4. Scanner or public user resolves the QR token.
5. Scanner records a scan event.
6. Admin updates package status or deactivates QR codes.

## 2. Layer Responsibilities

Controllers:

- Own HTTP routes, request binding, authorization attributes, and OpenAPI response metadata.
- Stay thin and call services for business behavior.
- Return responses through `ApiControllerBase.FromResult`.

Services:

- Own business validation and workflow decisions.
- Generate QR tokens and JWTs.
- Hash and verify passwords.
- Map entities to DTOs.
- Return `ApiResult<T>` for consistent HTTP status and error mapping.

Repositories:

- Encapsulate EF Core queries and persistence.
- Use eager loading only when the service requires related data.
- Use `AsNoTracking()` for read-only queries.
- Keep `SaveChangesAsync` explicit so service methods control transaction timing.

Data:

- `AppDbContext` defines entity sets, indexes, relationship behavior, and property constraints.
- Migrations define database evolution. Do not delete existing migrations.

Contracts:

- Request and response DTOs live under `Contracts/`.
- DTOs are public API contracts and must be treated as stable.
- `ApiErrorResponse` is the standard error contract.

Domain:

- Entities represent persisted state.
- Enums represent stored domain states and roles.

## 3. Folder Structure

```text
QR-Code Prototype/
  Contracts/
    Auth/
    Common/
    Packages/
    QrCodes/
  Controllers/
    Api/
      V1/
  Data/
  Domain/
    Entities/
    Enums/
  Migrations/
  Repositories/
  Services/
  Swagger/
  Program.cs
  appsettings.json
docs/
  API-CONTRACT.md
  ARCHITECTURE.md
  ARCHITECTURE-REVIEW.md
  TEST-PLAN.md
```

Folder ownership:

- `Contracts/` contains public API request/response models and shared error/result types.
- `Controllers/Api/V1/` contains versioned API controllers and the shared API base controller.
- `Data/` contains EF Core `AppDbContext`.
- `Domain/` contains persisted entities and enums.
- `Migrations/` contains EF Core migrations. Existing migrations must not be deleted.
- `Repositories/` contains persistence interfaces and EF-backed implementations.
- `Services/` contains business workflows and service interfaces.
- `Swagger/` contains OpenAPI example and documentation helpers.
- `docs/` contains frontend/API, architecture, review, and test planning documents.

## 4. Entity Relationship Overview

Entities:

- `Package`
  - Has one unique `PackageReference`.
  - Has optional `Description`.
  - Tracks status and core timestamps.
  - Has many `QrCodeRecord` rows.
- `QrCodeRecord`
  - Belongs to one package.
  - Stores a unique random token.
  - Stores the generated QR payload JSON.
  - Tracks active and expiry state.
  - Has many scan events.
- `QrScanEvent`
  - Records every scan attempt, including failures.
  - Keeps token, timestamp, IP address, user agent, success flag, and failure reason.
  - Uses a nullable QR relationship so historical scan rows can remain if a QR relationship is removed.
- `AppUser`
  - Stores local MVP users with email, password hash, role, and creation timestamp.

Indexes:

- `Package.PackageReference` unique.
- `QrCodeRecord.Token` unique.
- `AppUser.Email` unique.

Delete behavior:

- Deleting a package cascades to its QR records.
- QR scan events set their QR relationship to null when the related QR record is deleted.

## 5. API Design Rules

- All API endpoints use `/api/v1`.
- Existing public routes must not be renamed without a versioned replacement.
- Controllers must declare `ProducesResponseType` for success and documented error responses.
- Swagger operations must include a summary, description, request example when a request body exists, response example, and error examples.
- API errors must use:

```json
{
  "code": "error_code",
  "message": "Human-readable message.",
  "details": null
}
```

- Anonymous QR resolve and scan endpoints must remain anonymous unless a new versioned contract is introduced.
- Admin endpoints must use `[Authorize(Roles = "Admin")]`.
- Do not expose EF Core entities directly.

## 6. DTO Rules

- DTOs live under `Contracts/`.
- Request DTO validation should use data annotations for simple field-level rules.
- Response DTOs should be records when they are immutable projections.
- Additive fields are preferred over renames or removals.
- Breaking response changes require API versioning.
- Navigation properties must never appear in API responses.

## 7. Repository Rules

- Repositories contain data access only.
- Do not put business decisions in repositories.
- Use `AsNoTracking()` for read-only lookups.
- Use tracking queries only when the service will mutate the entity.
- Add purpose-specific methods when a query needs different tracking or includes.
- Keep save operations explicit.
- Avoid returning `IQueryable` from repositories.

## 8. Service Rules

- Services own business behavior and domain validation.
- Services should depend on interfaces so they can be unit tested.
- Services should avoid `HttpContext`; controllers pass HTTP-derived values explicitly.
- Services should map to DTOs before returning.
- Services should return `ApiResult<T>` and avoid throwing for expected domain failures.
- Cross-entity workflows should happen in one service method so the transaction boundary is obvious.

## 9. Authentication Model

Authentication is local MVP JWT bearer authentication.

- Passwords are hashed with ASP.NET Core `PasswordHasher<AppUser>`.
- JWTs contain user id, email, and role claims.
- JWT validation uses `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:SecretKey`.
- Admin authorization is currently role-string based through `[Authorize(Roles = "Admin")]`.

Production requirements:

- Move `Jwt:SecretKey` out of `appsettings.json`.
- Use a high-entropy secret managed through environment variables, user secrets, Key Vault, or another secret manager.
- Add account lifecycle controls, rate limiting, lockout, audit logging, password reset, and refresh-token strategy.
- Consider policy-based authorization before adding more roles.
- Consider external identity only after the MVP contract is stable.

## 10. QR Token Model

QR codes are modeled as server-side records with random public tokens.

- Tokens are generated with `RandomNumberGenerator.GetBytes(32)`.
- Tokens are Base64 URL-safe strings with `+`, `/`, and trailing `=` removed.
- Tokens are unique through a database unique index.
- Tokens are opaque public identifiers. They should not be derived from package ids or references.
- Tokens can be deactivated through `PATCH /api/v1/qr-codes/{id}/deactivate`.
- Tokens can expire through `expiresAtUtc`.
- Anonymous resolve and scan endpoints validate token existence, active state, and expiry before returning package data.

The safest frontend/printed QR design is token-only or URL-with-token:

```text
https://{frontend-host}/qr/{token}
```

The frontend should not depend on `payloadJson` for normal scanning. `payloadJson` is retained as backend-generated payload history for the MVP and admin inspection.

## 11. SQLite and EF Core Model

The MVP uses EF Core with SQLite through `AppDbContext`.

- Default connection string: `Data Source=qr-code-prototype.db`.
- Migrations live under `Migrations/`.
- `Package.PackageReference`, `QrCodeRecord.Token`, and `AppUser.Email` have unique indexes.
- Package-to-QR uses cascade delete.
- QR-to-scan-event uses set-null delete behavior to preserve scan history.
- Read-only repository queries should use `AsNoTracking()`.
- Queries requiring related package data should use explicit `Include`.

SQLite is suitable for local development and demos. Production should move to PostgreSQL or SQL Server before real multi-user logistics workloads.

## 12. Frontend Integration Assumptions

- The frontend calls the API directly using JSON.
- Authenticated requests send `Authorization: Bearer {jwt}`.
- JSON property names are camelCase.
- Enum values are numeric and documented in `docs/API-CONTRACT.md`.
- Frontend code should use `code` from `ApiErrorResponse` for error branching, not `message`.
- Anonymous QR resolve and scan flows are public by design.
- Admin package and QR management screens require an admin JWT.
- Package list endpoints are anonymous in the current MVP but should be treated as likely-to-be-protected in production.
- The QR rendering flow should use `token` or a route containing `token`; it should not require parsing `payloadJson`.
- API contracts are documented in `docs/API-CONTRACT.md`; frontend developers should not need to read backend code to integrate.
- Package list pagination is additive and backwards compatible: `GET /api/v1/packages` without query parameters returns the original array, while `pageNumber` or `pageSize` enables pagination headers with the same array response body.

## 13. Production Roadmap

Recommended sequence:

1. Add automated unit, integration, and API tests.
2. Add CI build and OpenAPI contract validation.
3. Move secrets to environment-specific secret storage.
4. Add rate limiting to anonymous resolve and scan endpoints.
5. Add package filtering and sorting beyond the current pagination support.
6. Add package location and scan history features using deliberate DTO additions.
7. Add structured logging and audit trails.
8. Add optimistic concurrency for package updates.
9. Move from SQLite to PostgreSQL or SQL Server for multi-user production workloads.
10. Introduce API versioning before breaking contract changes.

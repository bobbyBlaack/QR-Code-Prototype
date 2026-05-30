# Phase 2 Architecture Review

## 1. Entity Model Review

Strengths:

- Entities are small and understandable.
- `Package`, `QrCodeRecord`, `QrScanEvent`, and `AppUser` map cleanly to the MVP workflow.
- Unique references and tokens are enforced at the database level.
- Scan events support failed scan history because the QR relationship is nullable.

Weaknesses:

- No optimistic concurrency fields exist.
- Package does not yet model operational location, package weight, tracking number, or notes.
- Scan events capture IP and user agent but not structured scan location.

Technical debt:

- Time is set with `DateTime.UtcNow` directly in services.
- Domain entities are mutable property bags.

MVP acceptable risks:

- Mutable entities and direct timestamps are acceptable for a local MVP.
- Minimal package fields are acceptable while the frontend contract is being stabilized.

Production risks:

- Concurrent updates can overwrite each other.
- Package reference may not be enough for carrier or customer integrations.
- Lack of structured location history limits logistics reporting.

Recommended improvements:

- Add `TrackingNumber` when carrier-facing workflows are required.
- Add `Notes` only when there is a clear operator workflow for free-text annotations.
- Add `CurrentLocation` and `LastScannedAtUtc` when scan/location workflows are implemented.
- Add `Weight` when rate calculation, load planning, or carrier integration becomes part of scope.
- Add a concurrency token before multi-user production use.

## 2. Service Layer Review

Strengths:

- Services own business rules and map entities to DTOs.
- `ApiResult<T>` keeps expected failures explicit.
- Services are injected through interfaces and are mostly unit-testable.

Weaknesses:

- `AuthService` reads raw configuration and directly constructs `PasswordHasher`.
- `QrCodeService` directly uses randomness and wall-clock time.
- Some validation is duplicated between data annotations and service methods.

Technical debt:

- Error codes are string literals across services.
- No centralized clock or token generator abstraction.

MVP acceptable risks:

- Direct `DateTime.UtcNow` and random token generation are acceptable for the MVP.
- String error codes are acceptable while the error set is small.

Production risks:

- Harder deterministic testing.
- Error code drift as endpoint count grows.
- No transaction wrapper for future multi-step workflows beyond a single EF `SaveChangesAsync`.

Recommended improvements:

- Introduce `TimeProvider` or a clock abstraction.
- Introduce an `IQrTokenGenerator`.
- Centralize error code constants.
- Use options binding for JWT settings.

## 3. Repository Review

Strengths:

- Repositories hide EF Core from services.
- Read-list and token resolve queries use `AsNoTracking`.
- QR token resolution eagerly loads package data, avoiding an N+1 query.
- Save operations are explicit.

Weaknesses:

- Some methods are dual-purpose: tracked reads for updates and read-only display use the same method.
- No pagination exists for package lists.

Technical debt:

- No repository-level tests exist yet.
- Query methods do not express tracking intent in their names.

MVP acceptable risks:

- Simple repository methods are fine for the current endpoint set.
- Unpaginated package lists are acceptable only for small local datasets.

Production risks:

- Large package tables will make `GET /api/v1/packages` expensive.
- Ambiguous tracking intent can cause mistakes as more update flows are added.

Recommended improvements:

- Pagination has been added through backwards-compatible query parameters and response headers.
- Add filtering and sorting before production.
- Add purpose-specific tracked and no-tracking query methods as workflows grow.
- Add repository integration tests against SQLite migrations.

## 4. Controller Review

Strengths:

- Controllers are thin.
- Authorization attributes are visible at endpoint level.
- `ProducesResponseType` is already present on all endpoints.
- Standard result mapping is centralized in `ApiControllerBase`.

Weaknesses:

- Public package read endpoints are anonymous in the MVP.
- Controllers rely on Swagger operation filter examples rather than explicit per-action XML comments.

Technical debt:

- No API tests verify response metadata or error contract.

MVP acceptable risks:

- Anonymous package reads are acceptable only if demo data is non-sensitive.
- Thin controllers reduce risk despite minimal controller-specific logic.

Production risks:

- Package listing and package lookup may leak operational information if left anonymous.
- Missing API versioning will make future contract changes harder.

Recommended improvements:

- Add API tests for authentication and error contract.
- Add API versioning before any breaking change.
- Revisit package read authorization before real data is used.

## 5. Swagger Review

Strengths:

- Swagger is enabled in development.
- Bearer auth is documented.
- Operation filter provides summaries, descriptions, request examples, response examples, and error examples.

Weaknesses:

- Swagger examples are hand-maintained and can drift from DTOs.
- Swagger is disabled outside development.

Technical debt:

- No automated OpenAPI validation exists.

MVP acceptable risks:

- Hand-maintained examples are acceptable for the current small API surface.

Production risks:

- Drift between documentation and runtime behavior can break frontend integration.
- Swagger exposure policy must be decided per deployment environment.

Recommended improvements:

- Add OpenAPI generation and diff checks in CI.
- Add integration tests that validate critical documented examples.

## 6. DTO Review

Strengths:

- DTOs are separated from EF entities.
- Request DTOs use data annotations.
- Response DTOs are compact and stable.

Weaknesses:

- Enum values are serialized as numbers, which is compact but less self-describing.
- `CreateQrCodeRequest.AdditionalPayload` accepts `object?`, which is flexible but weakly typed.

Technical debt:

- No contract tests exist.

MVP acceptable risks:

- Numeric enums and flexible additional payloads are acceptable for internal MVP clients.

Production risks:

- Weakly typed QR payload additions can become inconsistent across clients.
- Changing enum serialization later would be breaking.

Recommended improvements:

- Keep numeric enums for this version and document them clearly.
- Replace or constrain `AdditionalPayload` when concrete logistics payload requirements emerge.
- Add API contract tests.

## 7. Authentication Review

Strengths:

- JWT issuer, audience, and signing key placeholders exist under `Jwt`.
- Passwords are hashed with ASP.NET Core `PasswordHasher`.
- Admin endpoints use role authorization.
- JWT challenge and forbidden responses now return the standard JSON error contract.

Weaknesses:

- The development secret is stored in `appsettings.json`.
- Registration allows selecting `Admin`, which is convenient for MVP but unsafe in production.
- No lockout, refresh token, password reset, email verification, or audit logging exists.

Technical debt:

- JWT settings are not bound to typed options.
- Role authorization is attribute-string based.

MVP acceptable risks:

- Local registration and local JWTs are acceptable for a prototype.

Production risks:

- Public admin self-registration is a critical production issue.
- Static secrets in configuration are unsafe.
- No rate limiting exposes login and scan endpoints to abuse.

Recommended improvements:

- Move secrets to environment variables or secret storage.
- Disable public admin self-registration before production.
- Add rate limiting and auth audit logs.
- Add policy-based authorization as roles grow.

## 8. SQLite Review

Strengths:

- SQLite keeps the MVP easy to run locally.
- EF migrations exist.
- Unique indexes and delete behavior are modeled.

Weaknesses:

- SQLite is not ideal for high-concurrency multi-user logistics operations.
- Database files are currently in the app project directory during local development.

Technical debt:

- No migration smoke test exists.

MVP acceptable risks:

- SQLite is acceptable for local development and demos.

Production risks:

- Write concurrency, backup, monitoring, and operational management are limited compared with PostgreSQL or SQL Server.

Recommended improvements:

- Keep SQLite for local development.
- Plan PostgreSQL or SQL Server for production.
- Add migration tests and environment-specific connection strings.

## 9. Security Review

Strengths:

- Admin mutation endpoints require JWT admin role.
- QR tokens are random and high entropy.
- Passwords are hashed.
- Standard error contract avoids leaking stack traces for expected failures.

Weaknesses:

- Anonymous resolve and scan endpoints are not rate limited.
- QR payload currently includes package reference.
- Local JWT secret is in plain configuration.
- No centralized exception-to-JSON handling for unexpected API exceptions.

Technical debt:

- No security headers or CORS policy are explicitly defined for API use.
- No audit trail for admin actions.

MVP acceptable risks:

- Anonymous QR token resolution is expected for QR scanning.
- Local secrets are acceptable only for local demo use.

Production risks:

- Token enumeration should remain impractical, but scan endpoints can still be spammed.
- Package metadata in QR payload can be copied from printed codes.
- Admin registration can create unauthorized privileged users if exposed.

Recommended improvements:

- Rate limit anonymous endpoints.
- Keep QR codes token-only in the printed/scanned value.
- Move secrets out of source-controlled config.
- Add structured audit logging.
- Add global exception middleware for API JSON errors.

## 10. Future Production Migration Review

Strengths:

- EF Core and DTO separation make migration feasible.
- Current API version prefix provides a base for versioning.
- Repository and service layers isolate most business logic from database provider choice.

Weaknesses:

- No provider-neutral integration test suite exists.
- No operational concerns are modeled yet: monitoring, backups, migrations pipeline, seeding, or deployment config.

Technical debt:

- SQLite-specific assumptions may appear if not tested against the target provider.

MVP acceptable risks:

- Provider migration can wait until the product workflow is validated.

Production risks:

- Late migration can expose differences in constraints, concurrency, date handling, and SQL translation.

Recommended improvements:

- Add production database decision before real customer data.
- Add migration pipeline and backup strategy.
- Test EF queries against the production provider before launch.

## Package Field Recommendations

Add now:

- None. The current package model is sufficient for the existing endpoints and frontend contract.

Add when workflow requires it:

- `TrackingNumber`: add when external carrier/customer tracking is in scope.
- `CurrentLocation`: add when scans or operations update structured locations.
- `LastScannedAtUtc`: add with scan/location workflow because it is derived operational state.
- `Notes`: add when operators need visible free-text annotations and moderation rules are clear.
- `Weight`: add when shipping cost, capacity planning, or compliance workflows require it.

Rationale:

- Adding fields now would expand the database and frontend contract without a current endpoint workflow.
- The next safest extension is additive DTO/entity fields with migrations after the frontend confirms actual screens and forms.

## QR Design Review

Should token remain random?

- Yes. Random high-entropy tokens are safer than predictable package identifiers.

Should payload remain JSON?

- Internally, yes. Stored JSON is convenient for audit/debug payload history.
- The printed or scanned QR value should not rely on rich JSON long term.

Should payload be encrypted?

- Not for the current MVP. Encryption adds key management complexity.
- If the QR contains only an opaque token, encryption is unnecessary because sensitive data is not embedded.

Should payload contain package information?

- The persisted payload may include limited package metadata for audit/history.
- The QR code presented to users should not embed package information.

Should QR contain only a token?

- Yes. The safest design is an opaque token or URL containing an opaque token.

Safest design:

- Printed QR contains only `https://{host}/qr/{token}` or the token itself.
- Backend resolves token, validates active/expiry state, records scans, and returns authorized data.
- Token is random, high entropy, revocable, and expires where needed.

Best logistics-industry design:

- Use an opaque token or standards-compatible tracking identifier as the public QR value.
- Keep operational package data server-side.
- Maintain scan history, location events, carrier references, and audit logs in backend systems.
- Support revocation, expiry, and rotation.

Implemented now:

- No breaking QR contract changes were made.
- Existing random token design was retained.
- Swagger and docs clarify the token-first architecture recommendation.

## Production Readiness Score

Score: `4/10`

Rationale:

- The backend has a coherent MVP architecture, stable DTO boundaries, Swagger documentation, EF Core persistence, and local JWT authentication.
- It is suitable for local demos and frontend contract development.
- It is not production-ready because authentication, secrets, rate limiting, audit logging, tests, pagination, operational monitoring, database provider choice, and concurrency handling still need hardening.

Primary blockers before production:

- Move JWT secret out of source-controlled configuration.
- Disable or control public admin registration.
- Add rate limiting for auth, QR resolve, and QR scan endpoints.
- Expand automated service, repository, API, and OpenAPI contract tests as new features are added.
- Add filtering and sorting to package list endpoints.
- Add structured logging and admin audit trails.
- Move from SQLite to PostgreSQL or SQL Server for multi-user workloads.
- Add API versioning before breaking changes.

## Recommended Next Phase

Phase 3 should be automated test foundation and contract verification.

Recommended scope:

1. Add unit test project for services.
2. Add integration test project for EF Core repositories and migrations.
3. Add API test project using `WebApplicationFactory<Program>`.
4. Assert the standard `ApiErrorResponse` contract across negative cases.
5. Generate or validate OpenAPI output in CI.
6. Add a lightweight smoke test for register, login, package creation, QR creation, resolve, scan, deactivate, and delete.

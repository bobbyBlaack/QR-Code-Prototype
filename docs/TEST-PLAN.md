# Test Plan

## Testability Review

The services are mostly unit-testable because controllers depend on service interfaces and services depend on repository interfaces. HTTP-only data such as IP address and user agent is passed into `QrCodeService.RecordScanAsync` by the controller, so the service can be tested without `HttpContext`.

Current strengths:

- Constructor injection is used throughout controllers, services, repositories, and EF Core.
- Service methods return deterministic `ApiResult<T>` values for expected failures.
- Repository interfaces allow services to be tested with mocks or fakes.
- DTO mapping happens in services, not controllers.

Current limitations:

- `AuthService` directly constructs `PasswordHasher<AppUser>` and reads `IConfiguration`.
- `QrCodeService` directly uses `RandomNumberGenerator` and `DateTime.UtcNow`.
- Repositories expose EF persistence through `SaveChangesAsync`, so repository integration tests are still needed.
- There is no test project yet.

## Manual Swagger Test Flow

Run locally:

```bash
dotnet restore "QR-Code Prototype/QR-Code Prototype.csproj"
dotnet build "QR-Code Prototype/QR-Code Prototype.csproj"
dotnet ef database update --project "QR-Code Prototype/QR-Code Prototype.csproj"
dotnet run --project "QR-Code Prototype/QR-Code Prototype.csproj"
```

Open `/swagger` and execute this manual flow:

1. `POST /api/v1/auth/register` with role `2` to create an admin.
2. Copy the returned JWT.
3. Use Swagger's `Authorize` button with `Bearer {token}`.
4. `GET /api/v1/auth/me` and verify the current user response.
5. `POST /api/v1/packages` and verify a package is created with status `1`.
6. `GET /api/v1/packages` and verify the package appears.
7. `GET /api/v1/packages?pageNumber=1&pageSize=1` and verify pagination headers.
8. `GET /api/v1/packages/{id}` and verify package details.
9. `PUT /api/v1/packages/{id}` and verify reference/description updates.
10. `PATCH /api/v1/packages/{id}/status` with status `2`, then status `3`.
11. `POST /api/v1/packages/{packageId}/qr-codes` and copy the token.
12. `GET /api/v1/qr-codes/{id}` and verify QR details.
13. Clear authorization or use an anonymous browser session.
14. `GET /api/v1/qr-codes/resolve/{token}` and verify limited package data.
15. `POST /api/v1/qr-codes/resolve/{token}/scan` and verify a successful scan response.
16. Re-authorize as admin.
17. `PATCH /api/v1/qr-codes/{id}/deactivate` and verify `isActive: false`.
18. Resolve the token again and verify `qr_code_inactive`.
19. `DELETE /api/v1/packages/{id}` and verify `204`.

## API Integration Test Strategy

Use `WebApplicationFactory<Program>` after adding a test project. Override the connection string to an isolated SQLite database and seed through public API calls where practical.

End-to-end happy path:

1. Register admin.
2. Login admin.
3. Create package.
4. Generate QR code.
5. Resolve token anonymously.
6. Record scan anonymously.
7. Update package status.
8. Deactivate QR code.
9. Resolve deactivated token and expect `qr_code_inactive`.

API contract scenarios:

- Every endpoint returns documented status codes.
- Every expected error response matches `ApiErrorResponse`.
- Protected endpoints return JSON `unauthorized` without a token.
- Admin endpoints return JSON `forbidden` for non-admin users.
- Anonymous QR resolve and scan endpoints work without a JWT.
- Request and response JSON property names remain camelCase.
- Package list pagination returns `X-Page-Number`, `X-Page-Size`, `X-Total-Count`, and `X-Total-Pages` headers.

## Service Unit Test Strategy

Use xUnit, FluentAssertions, and NSubstitute or Moq. Mock repository interfaces and assert `ApiResult<T>` values.

`PackageService`

- `GetAllAsync` returns DTOs from repository results.
- `GetByIdAsync` maps missing package to `package_not_found`.
- `CreateAsync` trims `packageReference`.
- `CreateAsync` rejects blank package reference.
- `CreateAsync` rejects duplicate package reference with `package_reference_exists`.
- `UpdateAsync` rejects missing package.
- `UpdateAsync` rejects duplicate package reference owned by another package.
- `UpdateStatusAsync` rejects invalid enum values.
- `UpdateStatusAsync` sets `deliveredAtUtc` when status is `Delivered`.
- `DeleteAsync` returns `204` for existing package and `package_not_found` for missing package.

`QrCodeService`

- `CreateForPackageAsync` rejects missing package with `package_not_found`.
- `CreateForPackageAsync` rejects past `expiresAtUtc`.
- Generated tokens are non-empty and URL-safe.
- Generated tokens are checked for uniqueness.
- Payload JSON includes `packageId`, `packageReference`, `token`, `createdAtUtc`, `expiresAtUtc`, and `additionalPayload`.
- `ResolveAsync` rejects missing token with `validation_error`.
- `ResolveAsync` rejects missing, inactive, and expired QR records.
- `DeactivateAsync` marks an existing QR code inactive.

`AuthService`

- `RegisterAsync` normalizes email to lower-case.
- `RegisterAsync` hashes passwords and never stores the raw password.
- `RegisterAsync` rejects duplicate email with `email_exists`.
- `RegisterAsync` rejects invalid role values.
- `LoginAsync` rejects unknown users and incorrect passwords with `invalid_credentials`.
- `LoginAsync` returns a JWT containing user id, email, and role claims.
- `GetCurrentUserAsync` rejects missing or invalid subject claim.

## Repository Test Strategy

Use SQLite with a temporary database file or an in-memory SQLite connection kept open for each test fixture. Run EF migrations before test execution.

Database tests:

- Database schema creates successfully from migrations.
- Unique index rejects duplicate `Package.PackageReference`.
- Unique index rejects duplicate `QrCodeRecord.Token`.
- Unique index rejects duplicate `AppUser.Email`.
- Package delete cascades to QR records.
- QR scan events preserve history when QR relationship is set null.

Repository query tests:

- `PackageRepository.GetAllAsync` returns newest packages first.
- `PackageRepository.GetPageAsync` returns the requested newest-first page.
- `PackageRepository.CountAsync` returns the total package count.
- `PackageRepository.GetByReferenceAsync` is no-tracking and does not interfere with tracked update flows.
- `QrCodeRepository.GetByIdAsync` includes package data.
- `QrCodeRepository.GetByTokenAsync` includes package data and uses no-tracking.
- `UserRepository.GetByEmailAsync` returns no-tracking users.

## Auth Test Strategy

Unit tests:

- Register requires valid email and password.
- Register enforces minimum password length through API validation.
- Register prevents duplicate normalized emails.
- Login fails with `invalid_credentials` for unknown user.
- Login fails with `invalid_credentials` for wrong password.
- Generated JWT uses configured issuer, audience, expiry, subject, email, and role.
- `GET /api/v1/auth/me` returns the current user for a valid token.

API tests:

- Missing bearer token returns `401 unauthorized`.
- Invalid bearer token returns `401 unauthorized`.
- Non-admin token on admin endpoint returns `403 forbidden`.
- Admin token on admin endpoint succeeds.

Production-hardening tests to add later:

- Login rate limit.
- Admin registration disabled or controlled.
- Password reset and account lockout when those features exist.

## QR Scan Test Cases

Successful scan:

- Existing active token records a scan event.
- Response has `wasSuccessful: true`.
- Response includes `resolvedPackage`.
- Scan event stores token, QR id, timestamp, IP address, and user agent.

Failed scan:

- Unknown token records a failed scan event.
- Missing token returns `validation_error`.
- Inactive QR returns `qr_code_inactive`.
- Expired QR returns `qr_code_expired`.
- Failed scan response uses standard error shape and includes scan details when available.

Operational scan behavior:

- Anonymous scan does not require JWT.
- Multiple scans of the same active QR create multiple scan events.
- Deactivated QR can no longer resolve successfully.
- Expired QR can no longer resolve successfully.

## Negative Test Cases

Validation:

- Missing `packageReference` returns `validation_error`.
- Too-long `packageReference` returns `validation_error`.
- Too-long `description` returns `validation_error`.
- Invalid package status enum returns `validation_error`.
- Past QR expiry returns `validation_error`.
- Invalid email returns `validation_error`.
- Missing password returns `validation_error`.

Not found:

- Missing package id returns `package_not_found`.
- Missing QR id returns `qr_code_not_found`.
- Missing QR token returns `qr_code_not_found`.
- Deleted package cannot be retrieved.

Conflict:

- Duplicate package reference returns `package_reference_exists`.
- Duplicate email returns `email_exists`.

Authorization:

- Package create without token returns `unauthorized`.
- Package update with user token returns `forbidden`.
- QR create without token returns `unauthorized`.
- QR deactivate with user token returns `forbidden`.

## Suggested Future Automated Test Project Structure

```text
tests/
  QR-Code-Prototype.UnitTests/
    Services/
      PackageServiceTests.cs
      QrCodeServiceTests.cs
      AuthServiceTests.cs
  QR-Code-Prototype.IntegrationTests/
    Data/
      MigrationTests.cs
      RepositoryTests.cs
  QR-Code-Prototype.ApiTests/
    AuthApiTests.cs
    PackagesApiTests.cs
    QrCodesApiTests.cs
    ErrorContractTests.cs
    SwaggerContractTests.cs
```

Unit tests should avoid EF Core. Integration tests should exercise EF Core and migrations. API tests should treat the backend as a black box and assert the frontend contract.

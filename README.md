# QR-Code-Prototype
This Repo is designed for us to collab and make the QR code prototype for JOI

## Mogaki API Scope

### What was added
- Local MVP backend API for logistics package QR codes.
- SQLite/EF Core code-first data model for packages, QR records, scan events, and app users.
- Repository and service layers for package, QR code, and auth workflows.
- Swagger/OpenAPI documentation and local JWT bearer authentication.

### How to run locally
```bash
dotnet restore "QR-Code Prototype/QR-Code Prototype.csproj"
dotnet run --project "QR-Code Prototype/QR-Code Prototype.csproj"
```

### How to run migrations
```bash
dotnet ef migrations add InitialApiModel --project "QR-Code Prototype/QR-Code Prototype.csproj"
dotnet ef database update --project "QR-Code Prototype/QR-Code Prototype.csproj"
```

### Swagger URL
Swagger runs at:

```text
https://localhost:7272/swagger
http://localhost:5046/swagger
```

### Main API flow
1. Register an admin or user with `POST /api/v1/auth/register`.
2. Login with `POST /api/v1/auth/login` and use the returned JWT as a Bearer token.
3. Create a package with `POST /api/v1/packages`.
4. List packages with optional pagination using `GET /api/v1/packages?pageNumber=1&pageSize=20`.
5. Generate a QR code with `POST /api/v1/packages/{packageId}/qr-codes`.
6. Resolve the QR token anonymously with `GET /api/v1/qr-codes/resolve/{token}`.
7. Record a scan with `POST /api/v1/qr-codes/resolve/{token}/scan`.
8. Update package status with `PATCH /api/v1/packages/{id}/status`.
9. Deactivate a QR code with `PATCH /api/v1/qr-codes/{id}/deactivate`.

### How to run tests
```bash
dotnet test "QR-Code Prototype.sln"
```

### Notes
JWT support is a local MVP model only. The signing key and authorization policies must be hardened before production use.

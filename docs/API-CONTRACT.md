# API Contract

Base URL: `http://localhost:5046`

Swagger: `/swagger`

All API routes use `/api/v1`. JSON property names are camelCase. Protected endpoints require:

```http
Authorization: Bearer {jwt}
```

## Standard Error Response

All expected API errors use this contract:

```json
{
  "code": "package_not_found",
  "message": "Package was not found.",
  "details": null
}
```

Validation errors use `code: "validation_error"` and may include a `details` object keyed by field name:

```json
{
  "code": "validation_error",
  "message": "Request validation failed.",
  "details": {
    "packageReference": [
      "The PackageReference field is required."
    ]
  }
}
```

## Enums

`PackageStatus`

| Value | Name |
| --- | --- |
| 1 | Created |
| 2 | InTransit |
| 3 | Delivered |
| 4 | Cancelled |
| 5 | Lost |

`UserRole`

| Value | Name |
| --- | --- |
| 1 | User |
| 2 | Admin |

## Packages

### GET /api/v1/packages

Method: `GET`

Route: `/api/v1/packages`

Purpose: List all packages ordered by creation time, newest first.

Authentication requirement: Anonymous in the current MVP. Frontend should isolate this behind an operational screen because this is likely to become authenticated.

Query parameters:

| Name | Required | Default | Notes |
| --- | --- | --- | --- |
| `pageNumber` | No | none | When supplied, must be greater than or equal to `1`. |
| `pageSize` | No | none | When supplied, must be between `1` and `100`. |

Request payload: none. If both query parameters are omitted, this endpoint preserves the original MVP behavior and returns the full package list. If either query parameter is supplied, the endpoint returns a paged list.

Response payload `200`:

```json
[
  {
    "id": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
    "packageReference": "PKG-10001",
    "description": "Box of replacement scanner batteries",
    "status": 1,
    "createdAtUtc": "2026-05-29T18:00:00Z",
    "updatedAtUtc": null,
    "deliveredAtUtc": null
  }
]
```

Pagination response headers when paging is requested:

| Header | Meaning |
| --- | --- |
| `X-Page-Number` | Current page number. |
| `X-Page-Size` | Current page size. |
| `X-Total-Count` | Total package count. |
| `X-Total-Pages` | Total page count. |

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 400 | `validation_error` | PageSize must be between 1 and 100. |

Frontend usage notes: Treat `status` as the numeric `PackageStatus` enum. Use `pageNumber` and `pageSize` for package list pagination. Read pagination metadata from response headers while keeping the body as an array for backwards compatibility.

### GET /api/v1/packages/{id}

Method: `GET`

Route: `/api/v1/packages/{id}`

Purpose: Retrieve one package by id.

Authentication requirement: Anonymous in the current MVP. Frontend should expect this to become authenticated.

Request payload: none

Response payload `200`:

```json
{
  "id": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
  "packageReference": "PKG-10001",
  "description": "Box of replacement scanner batteries",
  "status": 1,
  "createdAtUtc": "2026-05-29T18:00:00Z",
  "updatedAtUtc": null,
  "deliveredAtUtc": null
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 404 | `package_not_found` | Package was not found. |

Frontend usage notes: Use this endpoint for detail screens. Handle `404` by showing a not-found state.

### POST /api/v1/packages

Method: `POST`

Route: `/api/v1/packages`

Purpose: Create a new package record.

Authentication requirement: Admin Bearer JWT.

Request payload:

```json
{
  "packageReference": "PKG-10001",
  "description": "Box of replacement scanner batteries"
}
```

Response payload `201`:

```json
{
  "id": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
  "packageReference": "PKG-10001",
  "description": "Box of replacement scanner batteries",
  "status": 1,
  "createdAtUtc": "2026-05-29T18:00:00Z",
  "updatedAtUtc": null,
  "deliveredAtUtc": null
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 400 | `validation_error` | Request validation failed. |
| 401 | `unauthorized` | Authentication is required. |
| 403 | `forbidden` | You do not have permission to access this resource. |
| 409 | `package_reference_exists` | A package with this reference already exists. |

Frontend usage notes: Trim package reference before submit for better UX, but still rely on server validation.

### PUT /api/v1/packages/{id}

Method: `PUT`

Route: `/api/v1/packages/{id}`

Purpose: Update a package reference and description without changing its status.

Authentication requirement: Admin Bearer JWT.

Request payload:

```json
{
  "packageReference": "PKG-10001",
  "description": "Updated package description"
}
```

Response payload `200`:

```json
{
  "id": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
  "packageReference": "PKG-10001",
  "description": "Updated package description",
  "status": 1,
  "createdAtUtc": "2026-05-29T18:00:00Z",
  "updatedAtUtc": "2026-05-29T18:10:00Z",
  "deliveredAtUtc": null
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 400 | `validation_error` | Request validation failed. |
| 401 | `unauthorized` | Authentication is required. |
| 403 | `forbidden` | You do not have permission to access this resource. |
| 404 | `package_not_found` | Package was not found. |
| 409 | `package_reference_exists` | A package with this reference already exists. |

Frontend usage notes: This is a full update for editable package fields. Keep status changes on the status endpoint.

### PATCH /api/v1/packages/{id}/status

Method: `PATCH`

Route: `/api/v1/packages/{id}/status`

Purpose: Update only the package status.

Authentication requirement: Admin Bearer JWT.

Request payload:

```json
{
  "status": 2
}
```

Response payload `200`:

```json
{
  "id": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
  "packageReference": "PKG-10001",
  "description": "Box of replacement scanner batteries",
  "status": 2,
  "createdAtUtc": "2026-05-29T18:00:00Z",
  "updatedAtUtc": "2026-05-29T18:12:00Z",
  "deliveredAtUtc": null
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 400 | `validation_error` | Package status is invalid. |
| 401 | `unauthorized` | Authentication is required. |
| 403 | `forbidden` | You do not have permission to access this resource. |
| 404 | `package_not_found` | Package was not found. |

Frontend usage notes: When status is `3` (`Delivered`), the server sets `deliveredAtUtc`.

### DELETE /api/v1/packages/{id}

Method: `DELETE`

Route: `/api/v1/packages/{id}`

Purpose: Delete a package and cascade its QR records.

Authentication requirement: Admin Bearer JWT.

Request payload: none

Response payload `204`: none

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 401 | `unauthorized` | Authentication is required. |
| 403 | `forbidden` | You do not have permission to access this resource. |
| 404 | `package_not_found` | Package was not found. |

Frontend usage notes: Remove the package from local UI state after `204`. Show a not-found state if a concurrent delete already happened.

## QR Codes

### POST /api/v1/packages/{packageId}/qr-codes

Method: `POST`

Route: `/api/v1/packages/{packageId}/qr-codes`

Purpose: Generate a QR code record for a package.

Authentication requirement: Admin Bearer JWT.

Request payload:

```json
{
  "expiresAtUtc": "2026-06-30T23:59:59Z",
  "additionalPayload": {
    "route": "JHB-CPT"
  }
}
```

Response payload `201`:

```json
{
  "id": "11249289-d2b5-4ac9-955e-36d38bb4d26c",
  "packageId": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
  "token": "Q0XeRjfBkZQffxDznSXHkps2LXm46gL2qF3c2kM8zzk",
  "payloadJson": "{\"packageId\":\"4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834\",\"packageReference\":\"PKG-10001\",\"token\":\"Q0XeRjfBkZQffxDznSXHkps2LXm46gL2qF3c2kM8zzk\",\"createdAtUtc\":\"2026-05-29T18:00:00Z\"}",
  "isActive": true,
  "createdAtUtc": "2026-05-29T18:00:00Z",
  "expiresAtUtc": "2026-06-30T23:59:59Z"
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 400 | `validation_error` | ExpiresAtUtc must be in the future. |
| 401 | `unauthorized` | Authentication is required. |
| 403 | `forbidden` | You do not have permission to access this resource. |
| 404 | `package_not_found` | Package was not found. |

Frontend usage notes: Render the QR using `token` or a URL containing `token`. Do not require clients to parse `payloadJson`.

### GET /api/v1/qr-codes/{id}

Method: `GET`

Route: `/api/v1/qr-codes/{id}`

Purpose: Retrieve QR code details by id.

Authentication requirement: Admin Bearer JWT.

Request payload: none

Response payload `200`:

```json
{
  "id": "11249289-d2b5-4ac9-955e-36d38bb4d26c",
  "packageId": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
  "token": "Q0XeRjfBkZQffxDznSXHkps2LXm46gL2qF3c2kM8zzk",
  "payloadJson": "{\"packageId\":\"4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834\",\"packageReference\":\"PKG-10001\",\"token\":\"Q0XeRjfBkZQffxDznSXHkps2LXm46gL2qF3c2kM8zzk\",\"createdAtUtc\":\"2026-05-29T18:00:00Z\"}",
  "isActive": true,
  "createdAtUtc": "2026-05-29T18:00:00Z",
  "expiresAtUtc": null
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 401 | `unauthorized` | Authentication is required. |
| 403 | `forbidden` | You do not have permission to access this resource. |
| 404 | `qr_code_not_found` | QR code was not found. |

Frontend usage notes: This endpoint exposes token and payload data, so keep it on admin-only screens.

### GET /api/v1/qr-codes/resolve/{token}

Method: `GET`

Route: `/api/v1/qr-codes/resolve/{token}`

Purpose: Resolve an active QR token to limited package information.

Authentication requirement: Anonymous.

Request payload: none

Response payload `200`:

```json
{
  "qrCodeId": "11249289-d2b5-4ac9-955e-36d38bb4d26c",
  "packageId": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
  "packageReference": "PKG-10001",
  "description": "Box of replacement scanner batteries",
  "status": 1,
  "qrCreatedAtUtc": "2026-05-29T18:00:00Z",
  "qrExpiresAtUtc": null
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 400 | `validation_error` | Token is required. |
| 400 | `qr_code_inactive` | QR code is inactive. |
| 400 | `qr_code_expired` | QR code has expired. |
| 404 | `qr_code_not_found` | QR code token was not found. |

Frontend usage notes: This is the public scan landing lookup. Handle inactive, expired, and unknown QR codes with distinct user-facing states.

### POST /api/v1/qr-codes/resolve/{token}/scan

Method: `POST`

Route: `/api/v1/qr-codes/resolve/{token}/scan`

Purpose: Record a scan attempt for a QR token and return the scan result.

Authentication requirement: Anonymous.

Request payload:

```json
{
  "clientNote": "Scanned at receiving dock"
}
```

Response payload `200`:

```json
{
  "scanEventId": "65f9d689-0c73-4a9d-a2a3-bf0bbdc734cf",
  "wasSuccessful": true,
  "failureReason": null,
  "scannedAtUtc": "2026-05-29T18:05:00Z",
  "resolvedPackage": {
    "qrCodeId": "11249289-d2b5-4ac9-955e-36d38bb4d26c",
    "packageId": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
    "packageReference": "PKG-10001",
    "description": "Box of replacement scanner batteries",
    "status": 1,
    "qrCreatedAtUtc": "2026-05-29T18:00:00Z",
    "qrExpiresAtUtc": null
  }
}
```

Error responses use the standard error shape. For failed scans, `details` contains a `QrScanResponseDto` so the frontend can log the failed scan id.

| Status | Code | Example message |
| --- | --- | --- |
| 400 | `validation_error` | Token is required. |
| 400 | `qr_code_inactive` | QR code is inactive. |
| 400 | `qr_code_expired` | QR code has expired. |
| 404 | `qr_code_not_found` | QR code token was not found. |

Frontend usage notes: Call this when the scanner action should create an audit event. Use `resolvedPackage` for success UI and `details.scanEventId` for failed scan logging when present.

### PATCH /api/v1/qr-codes/{id}/deactivate

Method: `PATCH`

Route: `/api/v1/qr-codes/{id}/deactivate`

Purpose: Mark a QR code inactive so future resolve and scan attempts fail cleanly.

Authentication requirement: Admin Bearer JWT.

Request payload: none

Response payload `200`:

```json
{
  "id": "11249289-d2b5-4ac9-955e-36d38bb4d26c",
  "packageId": "4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834",
  "token": "Q0XeRjfBkZQffxDznSXHkps2LXm46gL2qF3c2kM8zzk",
  "payloadJson": "{\"packageId\":\"4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834\",\"packageReference\":\"PKG-10001\",\"token\":\"Q0XeRjfBkZQffxDznSXHkps2LXm46gL2qF3c2kM8zzk\",\"createdAtUtc\":\"2026-05-29T18:00:00Z\"}",
  "isActive": false,
  "createdAtUtc": "2026-05-29T18:00:00Z",
  "expiresAtUtc": null
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 401 | `unauthorized` | Authentication is required. |
| 403 | `forbidden` | You do not have permission to access this resource. |
| 404 | `qr_code_not_found` | QR code was not found. |

Frontend usage notes: Disable scan/print actions for a QR code once `isActive` is false.

## Auth

### POST /api/v1/auth/register

Method: `POST`

Route: `/api/v1/auth/register`

Purpose: Register a local MVP user and return a JWT.

Authentication requirement: Anonymous.

Request payload:

```json
{
  "email": "admin@example.com",
  "password": "Password123!",
  "role": 2
}
```

Response payload `201`:

```json
{
  "userId": "eb01c115-6c4f-421a-90cb-a3f0349079f6",
  "email": "admin@example.com",
  "role": 2,
  "token": "jwt-token",
  "expiresAtUtc": "2026-05-30T02:00:00Z"
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 400 | `validation_error` | Email and password are required. |
| 409 | `email_exists` | A user with this email already exists. |

Frontend usage notes: Admin self-registration is MVP-only. Do not expose this as an unrestricted production signup flow.

### POST /api/v1/auth/login

Method: `POST`

Route: `/api/v1/auth/login`

Purpose: Authenticate a local user and return a JWT.

Authentication requirement: Anonymous.

Request payload:

```json
{
  "email": "admin@example.com",
  "password": "Password123!"
}
```

Response payload `200`:

```json
{
  "userId": "eb01c115-6c4f-421a-90cb-a3f0349079f6",
  "email": "admin@example.com",
  "role": 2,
  "token": "jwt-token",
  "expiresAtUtc": "2026-05-30T02:00:00Z"
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 400 | `validation_error` | Request validation failed. |
| 401 | `invalid_credentials` | Invalid email or password. |

Frontend usage notes: Store the token according to the frontend security model. Send it as `Authorization: Bearer {token}` on protected endpoints.

### GET /api/v1/auth/me

Method: `GET`

Route: `/api/v1/auth/me`

Purpose: Return the current authenticated user represented by the JWT.

Authentication requirement: Bearer JWT.

Request payload: none

Response payload `200`:

```json
{
  "userId": "eb01c115-6c4f-421a-90cb-a3f0349079f6",
  "email": "admin@example.com",
  "role": 2,
  "token": "jwt-token",
  "expiresAtUtc": "2026-05-30T02:00:00Z"
}
```

Error responses:

| Status | Code | Example message |
| --- | --- | --- |
| 401 | `unauthorized` | JWT subject is missing or invalid. |
| 404 | `user_not_found` | User was not found. |

Frontend usage notes: Use this endpoint to validate an existing session after app load. The response includes a refreshed JWT generated by the current implementation.

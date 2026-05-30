using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QR_Code_Prototype.Swagger;

public sealed class ApiExamplesOperationFilter : IOperationFilter
{
    private static readonly Dictionary<string, EndpointExample> Examples = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GET api/v1/packages"] = new(
            "List packages",
            "Returns packages ordered by creation time. Optional pageNumber and pageSize query parameters enable backwards-compatible pagination with pagination metadata in response headers.",
            null,
            Array(Object(
                ("id", String("4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834")),
                ("packageReference", String("PKG-10001")),
                ("description", String("Box of replacement scanner batteries")),
                ("status", Integer(1)),
                ("createdAtUtc", String("2026-05-29T18:00:00Z")),
                ("updatedAtUtc", Null()),
                ("deliveredAtUtc", Null())))),

        ["GET api/v1/packages/{id}"] = new(
            "Get package by id",
            "Returns one package by its identifier.",
            null,
            PackageResponse()),

        ["POST api/v1/packages"] = new(
            "Create package",
            "Creates a package record. Admin JWT required for the MVP authorization model.",
            Object(("packageReference", String("PKG-10001")), ("description", String("Box of replacement scanner batteries"))),
            PackageResponse("Created package response.")),

        ["PUT api/v1/packages/{id}"] = new(
            "Update package",
            "Updates package reference and description without changing package status.",
            Object(("packageReference", String("PKG-10001")), ("description", String("Updated package description"))),
            PackageResponse("Updated package response.")),

        ["PATCH api/v1/packages/{id}/status"] = new(
            "Update package status",
            "Updates the package status. Use enum values: Created=1, InTransit=2, Delivered=3, Cancelled=4, Lost=5.",
            Object(("status", Integer(2))),
            PackageResponse("Package status updated.")),

        ["DELETE api/v1/packages/{id}"] = new(
            "Delete package",
            "Deletes a package and cascades related QR records. Admin JWT required.",
            null,
            null),

        ["POST api/v1/packages/{packageId}/qr-codes"] = new(
            "Create QR code for package",
            "Generates a secure random token and dynamic JSON payload for the package.",
            Object(("expiresAtUtc", String("2026-06-30T23:59:59Z")), ("additionalPayload", Object(("route", String("JHB-CPT"))))),
            QrCodeResponse()),

        ["GET api/v1/qr-codes/{id}"] = new(
            "Get QR code by id",
            "Returns QR code details including token and payload. Admin JWT required.",
            null,
            QrCodeResponse()),

        ["GET api/v1/qr-codes/resolve/{token}"] = new(
            "Resolve QR token",
            "Anonymous endpoint that validates a QR token and returns limited package data.",
            null,
            QrResolveResponse()),

        ["POST api/v1/qr-codes/resolve/{token}/scan"] = new(
            "Record QR scan",
            "Anonymous endpoint that records a scan attempt and returns the scan result.",
            Object(("clientNote", String("Scanned at receiving dock"))),
            QrScanResponse()),

        ["PATCH api/v1/qr-codes/{id}/deactivate"] = new(
            "Deactivate QR code",
            "Marks a QR code inactive so future resolve attempts fail cleanly.",
            null,
            QrCodeResponse("Deactivated QR code response.", false)),

        ["POST api/v1/auth/register"] = new(
            "Register local MVP user",
            "Registers a local MVP user and returns a JWT. Use role User=1 or Admin=2.",
            Object(("email", String("admin@example.com")), ("password", String("Password123!")), ("role", Integer(2))),
            AuthResponse()),

        ["POST api/v1/auth/login"] = new(
            "Login local MVP user",
            "Authenticates a local user and returns a JWT for direct frontend API calls.",
            Object(("email", String("admin@example.com")), ("password", String("Password123!"))),
            AuthResponse()),

        ["GET api/v1/auth/me"] = new(
            "Get current user",
            "Returns the current authenticated user based on the supplied JWT.",
            null,
            AuthResponse())
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var key = $"{context.ApiDescription.HttpMethod} {context.ApiDescription.RelativePath?.TrimEnd('/')}";
        if (!Examples.TryGetValue(key, out var example))
        {
            return;
        }

        operation.Summary = example.Summary;
        operation.Description = example.Description;

        if (example.Request is not null && operation.RequestBody?.Content.TryGetValue("application/json", out var requestContent) == true)
        {
            requestContent.Example = example.Request;
        }

        foreach (var response in operation.Responses)
        {
            if (!response.Value.Content.TryGetValue("application/json", out var content))
            {
                continue;
            }

            if (response.Key.StartsWith('2') && example.Response is not null)
            {
                content.Example = example.Response;
                continue;
            }

            var errorExamples = ErrorResponsesFor(key, response.Key);
            if (errorExamples.Count == 1)
            {
                content.Example = errorExamples[0];
                continue;
            }

            content.Examples = errorExamples
                .Select((errorExample, index) => new
                {
                    Key = $"error{index + 1}",
                    Value = new OpenApiExample { Value = errorExample }
                })
                .ToDictionary(item => item.Key, item => item.Value);
        }
    }

    private static OpenApiObject PackageResponse(string description = "Package response.") =>
        Object(
            ("id", String("4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834")),
            ("packageReference", String("PKG-10001")),
            ("description", String(description)),
            ("status", Integer(1)),
            ("createdAtUtc", String("2026-05-29T18:00:00Z")),
            ("updatedAtUtc", Null()),
            ("deliveredAtUtc", Null()));

    private static OpenApiObject QrCodeResponse(string description = "QR code response.", bool isActive = true) =>
        Object(
            ("id", String("11249289-d2b5-4ac9-955e-36d38bb4d26c")),
            ("packageId", String("4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834")),
            ("token", String("Q0XeRjfBkZQffxDznSXHkps2LXm46gL2qF3c2kM8zzk")),
            ("payloadJson", String("{\"packageId\":\"4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834\",\"packageReference\":\"PKG-10001\",\"token\":\"Q0XeRjfBkZQffxDznSXHkps2LXm46gL2qF3c2kM8zzk\",\"createdAtUtc\":\"2026-05-29T18:00:00Z\"}")),
            ("isActive", Boolean(isActive)),
            ("createdAtUtc", String("2026-05-29T18:00:00Z")),
            ("expiresAtUtc", Null()));

    private static OpenApiObject QrResolveResponse() =>
        Object(
            ("qrCodeId", String("11249289-d2b5-4ac9-955e-36d38bb4d26c")),
            ("packageId", String("4c8d2c20-90a9-4f4d-ae4e-bd708f7b33834")),
            ("packageReference", String("PKG-10001")),
            ("description", String("Box of replacement scanner batteries")),
            ("status", Integer(1)),
            ("qrCreatedAtUtc", String("2026-05-29T18:00:00Z")),
            ("qrExpiresAtUtc", Null()));

    private static OpenApiObject QrScanResponse() =>
        Object(
            ("scanEventId", String("65f9d689-0c73-4a9d-a2a3-bf0bbdc734cf")),
            ("wasSuccessful", Boolean(true)),
            ("failureReason", Null()),
            ("scannedAtUtc", String("2026-05-29T18:05:00Z")),
            ("resolvedPackage", QrResolveResponse()));

    private static OpenApiObject AuthResponse() =>
        Object(
            ("userId", String("eb01c115-6c4f-421a-90cb-a3f0349079f6")),
            ("email", String("admin@example.com")),
            ("role", Integer(2)),
            ("token", String("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.local-mvp-token")),
            ("expiresAtUtc", String("2026-05-30T02:00:00Z")));

    private static IReadOnlyList<OpenApiObject> ErrorResponsesFor(string endpointKey, string statusCode)
    {
        if (statusCode == "401")
        {
            return [Error("unauthorized", "Authentication is required.")];
        }

        if (statusCode == "403")
        {
            return [Error("forbidden", "You do not have permission to access this resource.")];
        }

        if (statusCode == "409" && endpointKey.Contains("auth/register", StringComparison.OrdinalIgnoreCase))
        {
            return [Error("email_exists", "A user with this email already exists.")];
        }

        if (statusCode == "409")
        {
            return [Error("package_reference_exists", "A package with this reference already exists.")];
        }

        if (statusCode == "404" && endpointKey.Contains("qr-codes", StringComparison.OrdinalIgnoreCase))
        {
            return [Error("qr_code_not_found", "QR code was not found.")];
        }

        if (statusCode == "404" && endpointKey.Contains("auth/me", StringComparison.OrdinalIgnoreCase))
        {
            return [Error("user_not_found", "User was not found.")];
        }

        if (statusCode == "404")
        {
            return [Error("package_not_found", "Package was not found.")];
        }

        if (statusCode == "400" && endpointKey.Contains("resolve/{token}", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                Error("validation_error", "Token is required."),
                Error("qr_code_inactive", "QR code is inactive."),
                Error("qr_code_expired", "QR code has expired.")
            ];
        }

        if (statusCode == "400")
        {
            return [Error("validation_error", "Request validation failed.")];
        }

        return [Error("error", "The request could not be completed.")];
    }

    private static OpenApiObject Error(string code, string message) =>
        Object(("code", String(code)), ("message", String(message)), ("details", Null()));

    private static OpenApiObject Object(params (string Name, IOpenApiAny Value)[] values)
    {
        var result = new OpenApiObject();
        foreach (var (name, value) in values)
        {
            result[name] = value;
        }

        return result;
    }

    private static OpenApiArray Array(params IOpenApiAny[] values)
    {
        var result = new OpenApiArray();
        foreach (var value in values)
        {
            result.Add(value);
        }

        return result;
    }

    private static OpenApiString String(string value) => new(value);
    private static OpenApiInteger Integer(int value) => new(value);
    private static OpenApiBoolean Boolean(bool value) => new(value);
    private static OpenApiNull Null() => new();

    private sealed record EndpointExample(string Summary, string Description, IOpenApiAny? Request, IOpenApiAny? Response);
}

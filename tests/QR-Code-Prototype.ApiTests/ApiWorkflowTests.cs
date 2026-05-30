using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace QR_Code_Prototype.ApiTests;

public sealed class ApiWorkflowTests : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiWorkflowTests(ApiTestApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Full_api_workflow_supports_auth_packages_qr_scan_and_pagination()
    {
        var token = await RegisterAndAuthorizeAdminAsync("admin-workflow@example.com");

        var createPackageResponse = await _client.PostAsJsonAsync("/api/v1/packages", new
        {
            packageReference = "PKG-WORKFLOW-1",
            description = "Workflow package"
        });
        Assert.Equal(HttpStatusCode.Created, createPackageResponse.StatusCode);
        var package = await JsonDocument.ParseAsync(await createPackageResponse.Content.ReadAsStreamAsync());
        var packageId = package.RootElement.GetProperty("id").GetGuid();

        var pageResponse = await _client.GetAsync("/api/v1/packages?pageNumber=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.True(pageResponse.Headers.Contains("X-Page-Number"));
        Assert.True(pageResponse.Headers.Contains("X-Total-Count"));
        var page = await JsonDocument.ParseAsync(await pageResponse.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Array, page.RootElement.ValueKind);

        var createQrResponse = await _client.PostAsJsonAsync($"/api/v1/packages/{packageId}/qr-codes", new
        {
            expiresAtUtc = DateTime.UtcNow.AddDays(1),
            additionalPayload = new { route = "JHB-CPT" }
        });
        Assert.Equal(HttpStatusCode.Created, createQrResponse.StatusCode);
        var qrCode = await JsonDocument.ParseAsync(await createQrResponse.Content.ReadAsStreamAsync());
        var qrCodeId = qrCode.RootElement.GetProperty("id").GetGuid();
        var tokenValue = qrCode.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(tokenValue));

        _client.DefaultRequestHeaders.Authorization = null;
        var resolveResponse = await _client.GetAsync($"/api/v1/qr-codes/resolve/{tokenValue}");
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);

        var scanResponse = await _client.PostAsJsonAsync($"/api/v1/qr-codes/resolve/{tokenValue}/scan", new
        {
            clientNote = "Receiving dock"
        });
        Assert.Equal(HttpStatusCode.OK, scanResponse.StatusCode);
        var scan = await JsonDocument.ParseAsync(await scanResponse.Content.ReadAsStreamAsync());
        Assert.True(scan.RootElement.GetProperty("wasSuccessful").GetBoolean());

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deactivateResponse = await _client.PatchAsync($"/api/v1/qr-codes/{qrCodeId}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
        var inactiveResponse = await _client.GetAsync($"/api/v1/qr-codes/resolve/{tokenValue}");
        Assert.Equal(HttpStatusCode.BadRequest, inactiveResponse.StatusCode);
        var inactive = await JsonDocument.ParseAsync(await inactiveResponse.Content.ReadAsStreamAsync());
        Assert.Equal("qr_code_inactive", inactive.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Protected_endpoint_returns_standard_unauthorized_error_without_token()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/packages", new
        {
            packageReference = "PKG-UNAUTHORIZED"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("unauthorized", error.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Admin_endpoint_returns_forbidden_for_user_role()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "user-role@example.com",
            password = "Password123!",
            role = 1
        });
        var auth = await JsonDocument.ParseAsync(await registerResponse.Content.ReadAsStreamAsync());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.RootElement.GetProperty("token").GetString());

        var response = await _client.PostAsJsonAsync("/api/v1/packages", new
        {
            packageReference = "PKG-FORBIDDEN"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("forbidden", error.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Validation_error_uses_standard_error_contract()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "not-an-email",
            password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("validation_error", error.RootElement.GetProperty("code").GetString());
        Assert.True(error.RootElement.TryGetProperty("details", out _));
    }

    [Fact]
    public async Task Swagger_json_is_available_in_development()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var swagger = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(swagger.RootElement.GetProperty("paths").TryGetProperty("/api/v1/packages", out _));
        Assert.True(swagger.RootElement.GetProperty("paths").TryGetProperty("/api/v1/auth/login", out _));
    }

    [Fact]
    public async Task Package_pagination_rejects_invalid_query_values()
    {
        var response = await _client.GetAsync("/api/v1/packages?pageNumber=0&pageSize=20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("validation_error", error.RootElement.GetProperty("code").GetString());
    }

    private async Task<string> RegisterAndAuthorizeAdminAsync(string email)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Password123!",
            role = 2
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var auth = await JsonDocument.ParseAsync(await registerResponse.Content.ReadAsStreamAsync());
        var token = auth.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token!;
    }
}

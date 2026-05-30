using Microsoft.Extensions.Configuration;
using QR_Code_Prototype.Contracts.Auth;
using QR_Code_Prototype.Domain.Entities;
using QR_Code_Prototype.Domain.Enums;
using QR_Code_Prototype.Repositories;
using QR_Code_Prototype.Services;

namespace QR_Code_Prototype.UnitTests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_hashes_password_and_normalizes_email()
    {
        var repository = new FakeUserRepository();
        var service = new AuthService(repository, Configuration());

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "ADMIN@EXAMPLE.COM",
            Password = "Password123!",
            Role = UserRole.Admin
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("admin@example.com", result.Value!.Email);
        Assert.Single(repository.Users);
        Assert.NotEqual("Password123!", repository.Users[0].PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));
    }

    [Fact]
    public async Task LoginAsync_rejects_invalid_credentials()
    {
        var service = new AuthService(new FakeUserRepository(), Configuration());

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "missing@example.com",
            Password = "Password123!"
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        Assert.Equal("invalid_credentials", result.Error!.Code);
    }

    [Fact]
    public async Task RegisterAsync_rejects_duplicate_email()
    {
        var repository = new FakeUserRepository();
        var service = new AuthService(repository, Configuration());
        await service.RegisterAsync(new RegisterRequest
        {
            Email = "admin@example.com",
            Password = "Password123!",
            Role = UserRole.Admin
        }, CancellationToken.None);

        var duplicate = await service.RegisterAsync(new RegisterRequest
        {
            Email = "ADMIN@example.com",
            Password = "Password123!",
            Role = UserRole.Admin
        }, CancellationToken.None);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal("email_exists", duplicate.Error!.Code);
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "QR-Code-Prototype",
                ["Jwt:Audience"] = "QR-Code-Prototype",
                ["Jwt:SecretKey"] = "UNIT_TEST_SECRET_KEY_WITH_AT_LEAST_32_CHARS"
            })
            .Build();

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<AppUser> Users { get; } = [];

        public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(user => user.Id == id));

        public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Users.FirstOrDefault(user => user.Email == email));

        public Task AddAsync(AppUser user, CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

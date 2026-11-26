using System.Net;
using System.Net.Http.Json;
using BMS.BuildingBlocks.Contracts.Authorization;
using BMS.Identity.Application.Contracts;
using BMS.Identity.Domain.Roles;
using BMS.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BMS.Identity.Tests.Integration;

public class AuthApiTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    [Fact]
    public async Task Seeded_admin_can_login_and_receives_all_permissions()
    {
        var client = factory.CreateClient();

        var login = await client.LoginAsync("admin", IdentityApiFactory.AdminPassword);

        Assert.False(string.IsNullOrEmpty(login.AccessToken));
        Assert.Contains(Role.AdministratorRoleName, login.User.Roles);
        Assert.Equal(Permissions.All.Select(p => p.Code).Order(), login.User.Permissions);
    }

    [Fact]
    public async Task Login_is_case_insensitive_for_username()
    {
        var login = await factory.CreateClient().LoginAsync("ADMIN", IdentityApiFactory.AdminPassword);

        Assert.Equal("admin", login.User.Username);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login", new LoginRequest("admin", "WrongPassword1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_user_returns_401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login", new LoginRequest("nobody", "WrongPassword1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Passwords_are_stored_hashed()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var admin = await db.Users.SingleAsync(u => u.NormalizedUsername == "ADMIN");

        Assert.DoesNotContain(IdentityApiFactory.AdminPassword, admin.PasswordHash);
        Assert.StartsWith("PBKDF2-SHA512$", admin.PasswordHash);
    }

    [Fact]
    public async Task Me_requires_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/api/identity/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Account_is_locked_after_too_many_failed_attempts()
    {
        var admin = await factory.CreateClient().AuthenticateAsync("admin", IdentityApiFactory.AdminPassword);
        await CreateUserAsync(admin, "locky", "Locky123x");

        var client = factory.CreateClient();
        for (var i = 0; i < IdentityApiFactory.MaxFailedAccessAttempts; i++)
        {
            await client.PostAsJsonAsync("/api/identity/auth/login", new LoginRequest("locky", "WrongPassword1"));
        }

        var response = await client.PostAsJsonAsync("/api/identity/auth/login", new LoginRequest("locky", "Locky123x"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_can_change_own_password()
    {
        var admin = await factory.CreateClient().AuthenticateAsync("admin", IdentityApiFactory.AdminPassword);
        await CreateUserAsync(admin, "changer", "Changer123x");
        var client = await factory.CreateClient().AuthenticateAsync("changer", "Changer123x");

        var wrong = await client.PostAsJsonAsync("/api/identity/auth/change-password", new ChangePasswordRequest("nope", "NewPass123x"));
        var ok = await client.PostAsJsonAsync("/api/identity/auth/change-password", new ChangePasswordRequest("Changer123x", "NewPass123x"));

        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        await factory.CreateClient().LoginAsync("changer", "NewPass123x");
    }

    private static async Task CreateUserAsync(HttpClient admin, string username, string password)
    {
        var response = await admin.PostAsJsonAsync(
            "/api/identity/users",
            new CreateUserRequest(username, $"{username}@example.com", username, password, []));
        response.EnsureSuccessStatusCode();
    }
}

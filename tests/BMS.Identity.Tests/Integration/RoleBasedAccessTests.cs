using System.Net;
using System.Net.Http.Json;
using BMS.BuildingBlocks.Contracts.Authorization;
using BMS.Identity.Application.Contracts;
using BMS.Identity.Domain.Roles;

namespace BMS.Identity.Tests.Integration;

public class RoleBasedAccessTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    [Fact]
    public async Task Admin_can_create_role_and_user_and_permissions_are_enforced()
    {
        var admin = await factory.CreateClient().AuthenticateAsync("admin", IdentityApiFactory.AdminPassword);

        var role = await PostAsync<RoleDto>(admin, "/api/identity/roles",
            new CreateRoleRequest("Viewer", "Read only", [Permissions.Identity.Users.View]));
        var user = await PostAsync<UserDto>(admin, "/api/identity/users",
            new CreateUserRequest("viewer", "viewer@example.com", "Viewer", "Viewer123x", [role.Id]));
        Assert.Equal("Viewer", Assert.Single(user.Roles).Name);

        var viewer = factory.CreateClient();
        await viewer.AuthenticateAsync("viewer", "Viewer123x");
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/identity/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/api/identity/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/identity/users",
            new CreateUserRequest("x", "x@example.com", "X", "Xxxxxxx1x", []))).StatusCode);

        // Granting a new permission to the role takes effect on the next sign-in.
        var updated = await PutAsync<RoleDto>(admin, $"/api/identity/roles/{role.Id}/permissions",
            new SetRolePermissionsRequest([Permissions.Identity.Users.View, Permissions.Identity.Roles.View]));
        Assert.Equal(2, updated.Permissions.Count);

        await viewer.AuthenticateAsync("viewer", "Viewer123x");
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/identity/roles")).StatusCode);
    }

    [Fact]
    public async Task Unknown_permissions_are_rejected()
    {
        var admin = await factory.CreateClient().AuthenticateAsync("admin", IdentityApiFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/identity/roles",
            new CreateRoleRequest("Bad", null, ["does.not.exist"]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_role_names_are_rejected()
    {
        var admin = await factory.CreateClient().AuthenticateAsync("admin", IdentityApiFactory.AdminPassword);

        await PostAsync<RoleDto>(admin, "/api/identity/roles", new CreateRoleRequest("Accounting", null, []));
        var response = await admin.PostAsJsonAsync("/api/identity/roles", new CreateRoleRequest("ACCOUNTING", null, []));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task System_roles_are_protected()
    {
        var admin = await factory.CreateClient().AuthenticateAsync("admin", IdentityApiFactory.AdminPassword);
        var roles = await admin.GetFromJsonAsync<List<RoleDto>>("/api/identity/roles");
        var administrator = roles!.Single(r => r.Name == Role.AdministratorRoleName);

        Assert.True(administrator.IsSystem);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/api/identity/roles/{administrator.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(
            $"/api/identity/roles/{administrator.Id}/permissions", new SetRolePermissionsRequest([]))).StatusCode);
    }

    [Fact]
    public async Task Last_administrator_cannot_lose_the_administrator_role()
    {
        var client = await factory.CreateClient().AuthenticateAsync("admin", IdentityApiFactory.AdminPassword);
        var myId = (await client.GetFromJsonAsync<UserProfileDto>("/api/identity/auth/me"))!.Id;

        var response = await client.PutAsJsonAsync($"/api/identity/users/{myId}/roles", new AssignRolesRequest([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Role_assigned_to_users_cannot_be_deleted_until_unassigned()
    {
        var admin = await factory.CreateClient().AuthenticateAsync("admin", IdentityApiFactory.AdminPassword);
        var role = await PostAsync<RoleDto>(admin, "/api/identity/roles", new CreateRoleRequest("Temp", null, []));
        var user = await PostAsync<UserDto>(admin, "/api/identity/users",
            new CreateUserRequest("temp", "temp@example.com", "Temp", "Temp1234x", [role.Id]));

        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/identity/roles/{role.Id}")).StatusCode);

        await PutAsync<UserDto>(admin, $"/api/identity/users/{user.Id}/roles", new AssignRolesRequest([]));
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/identity/roles/{role.Id}")).StatusCode);
    }

    [Fact]
    public async Task Non_administrators_cannot_escalate_to_or_take_over_administrator_accounts()
    {
        var admin = await factory.CreateClient().AuthenticateAsync("admin", IdentityApiFactory.AdminPassword);
        var adminId = (await admin.GetFromJsonAsync<UserProfileDto>("/api/identity/auth/me"))!.Id;
        var roles = await admin.GetFromJsonAsync<List<RoleDto>>("/api/identity/roles");
        var administratorRoleId = roles!.Single(r => r.Name == Role.AdministratorRoleName).Id;

        var role = await PostAsync<RoleDto>(admin, "/api/identity/roles", new CreateRoleRequest("User Manager", null,
            [Permissions.Identity.Users.View, Permissions.Identity.Users.Create, Permissions.Identity.Users.Update, Permissions.Identity.Users.Delete]));
        var manager = await PostAsync<UserDto>(admin, "/api/identity/users",
            new CreateUserRequest("usermanager", "usermanager@example.com", "User Manager", "Manager123x", [role.Id]));

        var client = await factory.CreateClient().AuthenticateAsync("usermanager", "Manager123x");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/identity/users/{manager.Id}/roles",
            new AssignRolesRequest([role.Id, administratorRoleId]))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/identity/users",
            new CreateUserRequest("sneaky", "sneaky@example.com", "Sneaky", "Sneaky123x", [administratorRoleId]))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/identity/users/{adminId}/reset-password",
            new ResetPasswordRequest("Hijacked123x"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/identity/users/{adminId}",
            new UpdateUserRequest("hijack@example.com", "Hijacked", false))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/identity/users/{adminId}")).StatusCode);

        // Managing non-administrator accounts is still allowed.
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/identity/users/{manager.Id}/reset-password",
            new ResetPasswordRequest("Manager456x"))).StatusCode);
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<T> PutAsync<T>(HttpClient client, string url, object body)
    {
        var response = await client.PutAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}

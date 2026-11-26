using BMS.BuildingBlocks.Domain.Exceptions;
using BMS.Identity.Domain.Roles;
using BMS.Identity.Domain.Users;

namespace BMS.Identity.Tests.Unit;

public class DomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void System_role_cannot_be_renamed_or_deleted()
    {
        var role = Role.Create("Administrator", null, isSystem: true);

        Assert.Throws<DomainException>(() => role.Update("Root", null));
        Assert.Throws<DomainException>(role.EnsureCanBeDeleted);

        role.Update("administrator", "new description");
        Assert.Equal("new description", role.Description);
    }

    [Fact]
    public void SetPermissions_replaces_existing_permissions()
    {
        var role = Role.Create("Sales", null);
        role.SetPermissions(["a", "b"]);
        role.SetPermissions(["b", "c", "c"]);

        Assert.Equal(["b", "c"], role.Permissions.Select(p => p.PermissionCode).Order());
    }

    [Fact]
    public void SetRoles_replaces_existing_roles()
    {
        var user = User.Create("jane", "jane@example.com", "Jane", "hash");
        var a = Role.Create("A", null);
        var b = Role.Create("B", null);

        user.SetRoles([a, b]);
        user.SetRoles([b]);

        Assert.Equal([b.Id], user.UserRoles.Select(ur => ur.RoleId));
    }

    [Fact]
    public void User_is_locked_out_after_max_failed_attempts_and_unlocked_after_duration()
    {
        var user = User.Create("jane", "jane@example.com", "Jane", "hash");

        user.RecordFailedLogin(Now, maxFailedAttempts: 2, TimeSpan.FromMinutes(5));
        Assert.False(user.IsLockedOut(Now));

        user.RecordFailedLogin(Now, maxFailedAttempts: 2, TimeSpan.FromMinutes(5));
        Assert.True(user.IsLockedOut(Now.AddMinutes(4)));
        Assert.False(user.IsLockedOut(Now.AddMinutes(6)));
    }

    [Fact]
    public void Username_is_normalized_for_case_insensitive_lookup()
    {
        var user = User.Create("  Jane.Doe ", "jane@example.com", "Jane", "hash");

        Assert.Equal("Jane.Doe", user.Username);
        Assert.Equal("JANE.DOE", user.NormalizedUsername);
    }
}

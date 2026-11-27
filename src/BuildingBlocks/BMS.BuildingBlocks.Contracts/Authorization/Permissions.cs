namespace BMS.BuildingBlocks.Contracts.Authorization;

/// <summary>
/// Central permission catalogue shared by all microservices.
/// <para>
/// Each module declares its permissions in a nested class and registers them in <see cref="All"/>.
/// The Identity service synchronises this catalogue so that permissions can be granted to roles,
/// and each microservice enforces them with <c>[HasPermission(Permissions.X.Y)]</c>.
/// </para>
/// </summary>
public static class Permissions
{
    public const string ClaimType = "permission";

    public static class Identity
    {
        public static class Users
        {
            public const string View = "identity.users.view";
            public const string Create = "identity.users.create";
            public const string Update = "identity.users.update";
            public const string Delete = "identity.users.delete";
        }

        public static class Roles
        {
            public const string View = "identity.roles.view";
            public const string Create = "identity.roles.create";
            public const string Update = "identity.roles.update";
            public const string Delete = "identity.roles.delete";
        }

        internal static IEnumerable<PermissionDefinition> Definitions =>
        [
            new(Users.View, "View users", "Users", "List and view user accounts."),
            new(Users.Create, "Create users", "Users", "Create new user accounts."),
            new(Users.Update, "Update users", "Users", "Edit users, assign roles and reset passwords."),
            new(Users.Delete, "Delete users", "Users", "Delete user accounts."),
            new(Roles.View, "View roles", "Roles", "List and view roles and permissions."),
            new(Roles.Create, "Create roles", "Roles", "Create new roles."),
            new(Roles.Update, "Update roles", "Roles", "Edit roles and their permissions."),
            new(Roles.Delete, "Delete roles", "Roles", "Delete roles."),
        ];
    }

    // Add new modules here, e.g.:
    // public static class Inventory { public static class Products { public const string View = "inventory.products.view"; } ... }

    /// <summary>
    /// Every permission known to the system.
    /// </summary>
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        .. Identity.Definitions,
    ];

    public static bool IsDefined(string code) =>
        All.Any(p => string.Equals(p.Code, code, StringComparison.Ordinal));
}

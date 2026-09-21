namespace SAFC.Expense.Domain.Authorization;


public static class SystemRoleDefinitions
{
    public sealed record Definition(
        string Code,
        string Name,
        string Description,
        bool IsSystem,
        IReadOnlyList<string> PermissionKeys);

    private static readonly Definition[] Definitions =
    [
        new(RoleCodes.SuperAdmin,
            "Super Administrator",
            "Holds every permission in the catalogue. Re-synced on every seed run.",
            IsSystem: true,
            PermissionCatalog.All.Select(entry => entry.Key).ToArray()),

        new(RoleCodes.Admin,
            "Administrator",
            "Manages users and assigns roles. Cannot change what a role grants.",
            IsSystem: false,
            [
                PermissionKeys.Users.View,
                PermissionKeys.Users.Create,
                PermissionKeys.Users.Update,
                PermissionKeys.Users.Remove,
                PermissionKeys.Users.AssignRoles,
                PermissionKeys.Roles.View,
                PermissionKeys.Permissions.View,
                PermissionKeys.Branches.View,
            ]),
    ];

    public static IReadOnlyList<Definition> All => Definitions;


    public static IReadOnlyList<string> AllReferencedKeys { get; } =
        Definitions.SelectMany(definition => definition.PermissionKeys)
                   .Distinct()
                   .ToArray();
}
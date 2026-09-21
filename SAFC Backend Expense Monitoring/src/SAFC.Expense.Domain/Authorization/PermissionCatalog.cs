namespace SAFC.Expense.Domain.Authorization;


public static class PermissionCatalog
{
    public sealed record Entry(string Key, string DisplayName, string Description, string Module);

    private static readonly Entry[] Entries =
    [
        new(PermissionKeys.Users.View,        "View Users",        "See the list of users and their details.",           PermissionKeys.Users.Module),
        new(PermissionKeys.Users.Create,      "Create User",       "Add a new user account.",                            PermissionKeys.Users.Module),
        new(PermissionKeys.Users.Update,      "Update User",       "Change a user's name, email or status.",             PermissionKeys.Users.Module),
        new(PermissionKeys.Users.Remove,      "Remove User",       "Deactivate a user account.",                         PermissionKeys.Users.Module),
        new(PermissionKeys.Users.AssignRoles, "Assign User Roles", "Grant and revoke a user's roles at a branch.",        PermissionKeys.Users.Module),

        new(PermissionKeys.Roles.View,        "View Roles",        "See the list of roles and their permissions.",        PermissionKeys.Roles.Module),
        new(PermissionKeys.Roles.Create,      "Create Role",       "Add a new role.",                                     PermissionKeys.Roles.Module),
        new(PermissionKeys.Roles.Update,      "Update Role",       "Change a role's name, description or permissions.",   PermissionKeys.Roles.Module),
        new(PermissionKeys.Roles.Remove,      "Remove Role",       "Deactivate a role.",                                  PermissionKeys.Roles.Module),

        new(PermissionKeys.Permissions.View,  "View Permissions",  "See the catalogue of permissions.",                   PermissionKeys.Permissions.Module),

        new(PermissionKeys.Branches.View,     "View Branches",     "See the list of branches.",                           PermissionKeys.Branches.Module),
    ];

    public static IReadOnlyList<Entry> All => Entries;
}

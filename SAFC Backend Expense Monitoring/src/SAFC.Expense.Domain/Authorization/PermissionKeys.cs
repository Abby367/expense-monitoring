namespace SAFC.Expense.Domain.Authorization;


public static class PermissionKeys
{
    public static class Users
    {
        public const string Module      = "users";

        public const string View        = "users.view";
        public const string Create      = "users.create";
        public const string Update      = "users.update";
        public const string Remove      = "users.remove";
        public const string AssignRoles = "users.assign-roles";
    }

    public static class Roles
    {
        public const string Module = "roles";

        public const string View   = "roles.view";
        public const string Create = "roles.create";
        public const string Update = "roles.update";
        public const string Remove = "roles.remove";
    }

    public static class Permissions
    {
        public const string Module = "permissions";

        public const string View   = "permissions.view";
    }

    public static class Branches
    {
        public const string Module = "branches";

        public const string View   = "branches.view";
    }
}

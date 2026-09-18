

namespace SAFC.Expense.Domain.Entities;

public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public Guid? GrantedById { get; private set; } 
    public DateTimeOffset CreatedAt { get; private set; }

    private RolePermission() { }

    internal RolePermission(Guid roleId, Guid permissionId, Guid? grantedByUserId, DateTimeOffset now)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedById = grantedByUserId;
        CreatedAt = now;
    }

}


namespace SAFC.Expense.Domain.Entities;

public sealed class Role 
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? RemovedById { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }
    public string? RemovedReason { get; private set; }
    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public const int NameMaxLength = 100;
    public const int CodeMaxLength = 50;
    public const int DescriptionMaxLength = 300;
    public const int RemovedReasonMaxLength = 500;
    public Guid? GrantedById { get; private set; }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    private Role() { }

    private Role(string code, string name, string description, bool isSystem, DateTimeOffset now) 
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Code = NormalizeCode(code);
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsSystem = isSystem;

        if (Code.Length > CodeMaxLength)
            throw new ArgumentException($"Code cannot exceed {CodeMaxLength} characters.", nameof(code));

        if (Name.Length > NameMaxLength)
            throw new ArgumentException($"Name cannot exceed {NameMaxLength} characters.", nameof(name));

        if (Description.Length > DescriptionMaxLength)
            throw new ArgumentException($"Description cannot exceed {DescriptionMaxLength} characters.", nameof(description));

        Id = Guid.CreateVersion7();
        CreatedAt = now;
    }
    

    public static Role Create(string code, string name, string description, DateTimeOffset now)
        => new(code, name, description, false, now);
    public static Role CreateSystem(string code, string name, string description, DateTimeOffset now)
        => new(code, name, description, true, now);

    public void Update(string name, string description, DateTimeOffset now)
    {
    EnsureNotRemoved();
    EnsureNotSystem();
    SetNameAndDescription(name, description);
    Touch(now);
    }

    public void SyncSystemDefinition(string name, string description,
        IEnumerable<Guid> permissionIds, Guid? grantedByUserId, DateTimeOffset now)
    {
        EnsureNotRemoved();
        if (!IsSystem)
            throw new InvalidOperationException("Use SetPermissions for non-system roles.");

        SetNameAndDescription(name, description);
        Reconcile(permissionIds, grantedByUserId, now);        // calls Touch at its end
    }
    public void SetPermissions(IEnumerable<Guid> permissionIds, Guid? grantedByUserId, DateTimeOffset now)
    {
    EnsureNotRemoved();
    EnsureNotSystem();
    Reconcile(permissionIds, grantedByUserId, now);
    }

    private void SetNameAndDescription(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmedName = name.Trim();
        if (trimmedName.Length > NameMaxLength)
            throw new ArgumentException($"Name cannot exceed {NameMaxLength} characters.", nameof(name));

        var trimmedDescription = description?.Trim() ?? string.Empty;
        if (trimmedDescription.Length > DescriptionMaxLength)
            throw new ArgumentException($"Description cannot exceed {DescriptionMaxLength} characters.", nameof(description));

        Name = trimmedName;
        Description = trimmedDescription;
    }

    private void Reconcile(IEnumerable<Guid> permissionIds, Guid? grantedByUserId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(permissionIds);

        var desired = permissionIds.ToHashSet();
        if (desired.Contains(Guid.Empty))
            throw new ArgumentException("...", nameof(permissionIds));

        _rolePermissions.RemoveAll(rp => !desired.Contains(rp.PermissionId));

        var existing = _rolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var id in desired.Except(existing))
            _rolePermissions.Add(new RolePermission(Id, id, grantedByUserId, now));

        Touch(now);
    }
    public void Remove(Guid removedByUserId, string reason, DateTimeOffset now)
    {
        EnsureNotRemoved();
        EnsureNotSystem();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (removedByUserId == Guid.Empty)
            throw new ArgumentException("The user who removed this role cannot be empty.", nameof(removedByUserId));
        var trimmedReason = reason.Trim();
        if (trimmedReason.Length > RemovedReasonMaxLength)
            throw new ArgumentException($"Removed reason cannot exceed {RemovedReasonMaxLength} characters.", nameof(reason));
        RemovedById = removedByUserId;
        RemovedAt = now;
        RemovedReason = trimmedReason;
        Touch(now);
    }
    private void EnsureNotRemoved()
    {
        if (RemovedAt is not null)
            throw new InvalidOperationException("Role has been removed.");
    }
    private void EnsureNotSystem()
    {
        if (IsSystem)
            throw new InvalidOperationException("Cannot modify a system role.");
    }
    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
    }
}
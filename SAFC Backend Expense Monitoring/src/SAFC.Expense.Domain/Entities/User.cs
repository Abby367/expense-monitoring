using SAFC.Expense.Domain.Enums;

namespace SAFC.Expense.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public UserStatus Status { get; private set; }

    public bool MustChangePassword { get; private set; }
    public DateTimeOffset? InviteEmailSentAt { get; private set; }

    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? TemporaryPasswordExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? RemovedById { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }
    public bool IsRemoved => RemovedAt is not null;
    public string? RemovedReason { get; private set; }

    public static readonly TimeSpan TemporaryPasswordValidity = TimeSpan.FromHours(48);
    public AuthMethod AuthMethod { get; private set; }
    public string? MicrosoftId { get; private set; }
    public DateTimeOffset? FirstLoggedInAt { get; private set; }
    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public const int EmailMaxLength = 256;
    public const int FullNameMaxLength = 200;
    public const int RemovedReasonMaxLength = 500;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private User() { }

    private User(string email, string fullName,
        AuthMethod authMethod, Guid? createdByUserId, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        Email = NormalizeEmail(email);
        FullName = fullName.Trim();

        if (Email.Length > EmailMaxLength)
            throw new ArgumentException($"Email cannot exceed {EmailMaxLength} characters.", nameof(email));

        if (FullName.Length > FullNameMaxLength)
            throw new ArgumentException($"Full name cannot exceed {FullNameMaxLength} characters.", nameof(fullName));

        if (!Email.Contains('@'))
            throw new ArgumentException("Email is not a valid address.", nameof(email));

        Id = Guid.CreateVersion7();
        AuthMethod = authMethod;
        Status = UserStatus.Invited;
        CreatedByUserId = createdByUserId;
        CreatedAt = now;
    }

    public static User CreateByAdminWithPassword(string email, string fullName,
    string temporaryPasswordHash, Guid? createdByUserId, DateTimeOffset now)
    {
        var user = new User(email, fullName, AuthMethod.Password, createdByUserId, now);
        user.SetTemporaryPassword(temporaryPasswordHash, now);
        return user;
    }

    public static User CreateByAdminWithMicrosoft(string email, string fullName,
        Guid? createdByUserId, DateTimeOffset now)
               => new(email, fullName, AuthMethod.Microsoft, createdByUserId, now);



    public void ResetPassword(string temporaryPasswordHash, DateTimeOffset now)
    {
        EnsureNotRemoved();
        if (AuthMethod != AuthMethod.Password)
        {
            throw new InvalidOperationException(
                "This account signs in with Microsoft and has no password.");
        }

        SetTemporaryPassword(temporaryPasswordHash, now);
        Touch(now);
    }

    public void ChangePassword(string newPasswordHash, DateTimeOffset now)
    {
        EnsureNotRemoved();
        if (AuthMethod != AuthMethod.Password)
            throw new InvalidOperationException(
            "This account signs in with Microsoft and has no password.");
        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        TemporaryPasswordExpiresAt = null;
        Touch(now);
    }

    public void Activate(DateTimeOffset now)
    {
        EnsureNotRemoved();

        Status = UserStatus.Active;
        Touch(now);
    }

    public void Suspend(DateTimeOffset now)
    {
        EnsureNotRemoved();

        Status = UserStatus.Suspended;
        Touch(now);
    }

    public void MarkInviteEmailSent(DateTimeOffset sentAt)
    {
        EnsureNotRemoved();
        InviteEmailSentAt = sentAt;
        Touch(sentAt);
    }
    private void SetTemporaryPassword(string hash, DateTimeOffset now)
    {
        PasswordHash = hash;
        MustChangePassword = true;
        TemporaryPasswordExpiresAt = now + TemporaryPasswordValidity;
    }
    public void Remove(Guid removedByUserId, string reason, DateTimeOffset now)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (removedByUserId == Guid.Empty)
            throw new ArgumentException("The user who removed this account cannot be empty.", nameof(removedByUserId));


        var trimmedReason = reason.Trim();

        if (trimmedReason.Length > RemovedReasonMaxLength)
            throw new ArgumentException(
                $"Removed reason cannot exceed {RemovedReasonMaxLength} characters.", nameof(reason));

        EnsureNotRemoved();
        RemovedAt = now;
        RemovedById = removedByUserId;
        RemovedReason = trimmedReason;
        Touch(now);
    }
    private void EnsureNotRemoved()
    {
        if (IsRemoved)
            throw new InvalidOperationException("This user has been removed.");
    }

    public UserRole Grant(Guid roleId, Guid? branchId, Guid? grantedByUserId, DateTimeOffset now)
    {
        EnsureNotRemoved();

        
        var alreadyHeld = _userRoles.Any(grant =>
            !grant.IsRemoved && grant.RoleId == roleId && grant.BranchId == branchId);

        if (alreadyHeld)
            throw new InvalidOperationException("This user already holds that role at that scope.");

        var granted = new UserRole(Id, roleId, branchId, grantedByUserId, now);
        _userRoles.Add(granted);

        return granted;
    }

    public void RevokeGrant(Guid grantId, Guid removedByUserId, string reason, DateTimeOffset now)
    {
        EnsureNotRemoved();

        var grant = _userRoles.SingleOrDefault(g => g.Id == grantId)
            ?? throw new InvalidOperationException("That grant does not belong to this user.");

        grant.Remove(removedByUserId, reason, now);
    }



    private void Touch(DateTimeOffset now) => UpdatedAt = now;


}

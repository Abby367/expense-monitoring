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
    public string? RemovedReason { get; private set; }
    public static readonly TimeSpan TemporaryPasswordValidity = TimeSpan.FromHours(48);
    public AuthMethod AuthMethod { get; private set; }
    public string? MicrosoftId { get; private set; }
    public DateTimeOffset? FirstLoggedInAt { get; private set; }

    public const int EmailMaxLength = 256;
    public const int FullNameMaxLength = 200;
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
        Status = UserStatus.Active;
        Touch(now);
    }

    public void Suspend(DateTimeOffset now)
    {
        Status = UserStatus.Suspended;
        Touch(now);
    }

    public void MarkInviteEmailSent(DateTimeOffset sentAt)
    {
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
    Status = UserStatus.Removed;
    RemovedAt = now;
    RemovedById = removedByUserId;
    RemovedReason = reason;
    Touch(now);
}


    private void Touch(DateTimeOffset now) => UpdatedAt = now;

}

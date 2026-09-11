namespace SAFC.Expense.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool MustChangePassword { get; private set; }
    private User() { }   

     private User(string email, string passwordHash, string fullName)
    {
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        IsActive = true;
        MustChangePassword = true;
    }

    public static User Create(string email, string passwordHash, string fullName)
    => new(email, passwordHash, fullName);

}
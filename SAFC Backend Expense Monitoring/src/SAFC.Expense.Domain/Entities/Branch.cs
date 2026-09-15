

namespace SAFC.Expense.Domain.Entities;

public sealed class Branch
{
     public Guid Id { get; private set; }
     public string Code { get; private set; } = string.Empty;
     public string Name { get; private set; } = string.Empty;

     public DateTimeOffset CreatedAt { get; private set; }
     public DateTimeOffset? UpdatedAt { get; private set; }
     public Guid? RemovedById { get; private set; }
     public DateTimeOffset? RemovedAt { get; private set; }
     public bool IsRemoved => RemovedAt is not null;
     public string? RemovedReason { get; private set; }

     public const int CodeMaxLength = 20;
     public const int NameMaxLength = 150;
     public const int RemovedReasonMaxLength = 500;

     public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

     private Branch() { }      


    private Branch (string code, string name, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Code = NormalizeCode(code);        
        Name = name.Trim();

        if (Code.Length > CodeMaxLength)
            throw new ArgumentException($"Code cannot exceed {CodeMaxLength} characters.", nameof(code));

        if (Name.Length > NameMaxLength)
            throw new ArgumentException($"Name cannot exceed {NameMaxLength} characters.", nameof(name));

        Id = Guid.CreateVersion7();
        CreatedAt = now;
     
    }
    public static Branch Create(string code, string name, DateTimeOffset now)
        => new(code, name, now);
       

    public void Rename(string name, DateTimeOffset now)
    {
        EnsureNotRemoved();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        name = name.Trim();
        if (name.Length > NameMaxLength)
            throw new ArgumentException($"Name cannot exceed {NameMaxLength} characters.", nameof(name));
        Name = name;
        Touch(now);
        
    }
    public void Remove(Guid removedByUserId, string reason, DateTimeOffset now)
    {
        EnsureNotRemoved();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (removedByUserId == Guid.Empty)
            throw new ArgumentException("The user who removed this branch cannot be empty.", nameof(removedByUserId));

        var trimmedReason = reason.Trim();

        if (trimmedReason.Length > RemovedReasonMaxLength)
        throw new ArgumentException(
        $"Removed reason cannot exceed {RemovedReasonMaxLength} characters.", nameof(reason));


       
        RemovedAt = now;
        RemovedById = removedByUserId;
        RemovedReason = trimmedReason;
        Touch(now);
    }
    private void EnsureNotRemoved()
    {
        if (IsRemoved)
            throw new InvalidOperationException("This branch has been removed.");
    }
     private void Touch(DateTimeOffset now) => UpdatedAt = now;

}
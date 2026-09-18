

namespace SAFC.Expense.Domain.Entities;

public sealed class Permission
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    
    public string Module { get; private set; } = string.Empty;


    public const int KeyMaxLength = 100;
    public const int DescriptionMaxLength = 300;
    public const int ModuleMaxLength = 50;

    public static string NormalizeKey(string key) => key.Trim().ToLowerInvariant();
    private Permission() { }
    private Permission(string key, string description, string module)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);

        Key = NormalizeKey(key);
        Description = description.Trim();
        Module = module.Trim();

        if (Key.Length > KeyMaxLength)
            throw new ArgumentException($"Key cannot exceed {KeyMaxLength} characters.", nameof(key));

        if (Description.Length > DescriptionMaxLength)
            throw new ArgumentException($"Description cannot exceed {DescriptionMaxLength} characters.", nameof(description));

        if (Module.Length > ModuleMaxLength)
            throw new ArgumentException($"Module cannot exceed {ModuleMaxLength} characters.", nameof(module));

        Id = Guid.CreateVersion7();
    }
    public static Permission Create(string key, string description, string module)
        => new(key, description, module);

}
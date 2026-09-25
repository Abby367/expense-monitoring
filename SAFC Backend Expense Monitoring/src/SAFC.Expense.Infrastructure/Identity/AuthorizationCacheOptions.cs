namespace SAFC.Expense.Infrastructure.Identity;

public sealed class AuthorizationCacheOptions
{
    public const string SectionName = "Authorization";


    public int SnapshotSeconds { get; init; } = 10;
}

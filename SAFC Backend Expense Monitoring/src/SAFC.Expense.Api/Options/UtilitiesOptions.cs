namespace SAFC.Expense.Api.Options;


public sealed class UtilitiesOptions
{
    public const string SectionName = "Utilities";

    public string SeedDefaultsConfirmPhrase { get; init; } = string.Empty;
    public string SuperAdminEmail { get; init; } = string.Empty;

}

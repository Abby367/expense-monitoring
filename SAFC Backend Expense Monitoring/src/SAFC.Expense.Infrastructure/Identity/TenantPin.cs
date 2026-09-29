namespace SAFC.Expense.Infrastructure.Identity;

internal static class TenantPin
{

    internal const string SafcTenantId = "";

    internal const string PersonalAccountsTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";


    internal static string? Validate(string pinned, string? configured, bool isDevelopment)
    {

        var effective = Effective(pinned, configured, isDevelopment);


        if (string.IsNullOrWhiteSpace(effective))
            return "SAFC's Entra tenant id has not been confirmed. Set TenantPin.SafcTenantId "
                 + "once the tenant admin confirms it"
                 + (isDevelopment ? ", or set Microsoft__TenantId for local development." : ".");

        if (!Guid.TryParse(effective, out _))
            return $"Entra tenant id '{effective}' is not a GUID.";

        if (string.Equals(effective, PersonalAccountsTenantId, StringComparison.OrdinalIgnoreCase))
            return $"Entra tenant id '{effective}' is the personal-Microsoft-accounts tenant, "
                 + "not SAFC's directory. It would admit every consumer Microsoft account.";

        return null;
    }
   internal static string Effective(string pinned, string? configured, bool isDevelopment) =>
        isDevelopment && !string.IsNullOrWhiteSpace(configured)
            ? configured.Trim()
            : pinned.Trim();


}

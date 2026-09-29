namespace SAFC.Expense.Infrastructure.Identity;

public sealed class MicrosoftAuthOptions
{
    public const string SectionName = "Microsoft";

    // set, not init, unlike the other options classes: TenantId is COMPUTED at registration
    // (the pin outside Development), not bound from configuration. And internal, not public,
    // so the binder cannot reach it — a stray GetSection(...).Bind(...) silently overwrites a
    // public setter and skips this one entirely. Measured, not assumed.
    public string TenantId { get; internal set; } = string.Empty;
    public string ClientId { get; internal set; } = string.Empty;

    // Derived, never configured. Two configured URLs can disagree with the tenant id;
    // one tenant id cannot disagree with itself. This is the whole point of the class.
    // Two properties rather than one because JwtBearer takes them as separate settings, and
    // a v1-vs-v2 issuer split would move one without the other.
    public string Authority => $"https://login.microsoftonline.com/{TenantId}/v2.0";
    public string Issuer => $"https://login.microsoftonline.com/{TenantId}/v2.0";
}

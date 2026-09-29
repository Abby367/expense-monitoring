using SAFC.Expense.Infrastructure.Identity;

namespace SAFC.Expense.Tests;

public class TenantPinTests
{
    // Deliberately arbitrary. Using the unconfirmed d1c368b8-… here would read as endorsing it.
    private const string AnyTenant = "11111111-2222-3333-4444-555555555555";

    [Fact]
    public void The_Personal_Accounts_Tenant_Is_Refused_By_Name()
    {
        var error = TenantPin.Validate(
            pinned: "", configured: TenantPin.PersonalAccountsTenantId, isDevelopment: true);

        Assert.NotNull(error);

        // The message has to name what it is. "Invalid tenant" would send someone hunting for
        // a typo in a GUID that is perfectly well-formed.
        Assert.Contains("personal", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_Unconfirmed_Pin_Refuses_To_Start_In_Any_Environment()
    {
        var inDevelopment = TenantPin.Validate(pinned: "", configured: null, isDevelopment: true);
        var inProduction = TenantPin.Validate(pinned: "", configured: null, isDevelopment: false);

        // Not just "refuses" — refuses with the message that names the next action. Guid.TryParse
        // would reject a blank value anyway, so without these the blank check is dead code that
        // only changes what the operator reads.
        Assert.Contains("not been confirmed", inDevelopment);
        Assert.Contains("not been confirmed", inProduction);
    }

    [Theory]
    [InlineData("common")]
    [InlineData("organizations")]
    [InlineData("consumers")]
    public void A_Tenant_Alias_Is_Refused(string alias)
    {
        Assert.NotNull(TenantPin.Validate(pinned: "", alias, isDevelopment: true));
    }

    [Fact]
    public void Development_Accepts_A_Configured_Tenant()
    {
        Assert.Null(TenantPin.Validate(pinned: "", AnyTenant, isDevelopment: true));
    }

    [Fact]
    public void Development_Falls_Back_To_The_Pin_When_Nothing_Is_Configured()
    {
        Assert.Null(TenantPin.Validate(pinned: AnyTenant, configured: null, isDevelopment: true));
    }

    [Fact]
    public void Outside_Development_Configuration_Is_Ignored()
    {
        // A perfectly valid configured tenant must NOT rescue an unconfirmed pin. This is the
        // property the pin exists for: a wrong tenant in a deployed .env cannot take effect.
        Assert.NotNull(TenantPin.Validate(pinned: "", AnyTenant, isDevelopment: false));

        // And the pin wins even when configuration carries the dangerous value.
        Assert.Null(TenantPin.Validate(
            pinned: AnyTenant,
            configured: TenantPin.PersonalAccountsTenantId,
            isDevelopment: false));
    }
    [Fact]
    public void Development_Prefers_Configuration_Over_A_Non_Blank_Pin()
    {
        // Every other Development test passes pinned: "" — so none of them would notice if
        // Effective started preferring the pin. This is the only shape that exists once the
        // tenant admin confirms the id and SafcTenantId stops being empty.
        Assert.Equal(
            AnyTenant,
            TenantPin.Effective(
                pinned: TenantPin.PersonalAccountsTenantId,
                configured: AnyTenant,
                isDevelopment: true));
    }


}

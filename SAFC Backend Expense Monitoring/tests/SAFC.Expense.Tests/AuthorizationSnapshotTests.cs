using System.Reflection;
using SAFC.Expense.Application.Common.Authorization;
using SAFC.Expense.Domain.Authorization;

namespace SAFC.Expense.Tests;

public class AuthorizationSnapshotTests
{
    private static readonly Guid Person = Guid.CreateVersion7();
    private static readonly Guid Makati = Guid.CreateVersion7();
    private static readonly Guid Cebu = Guid.CreateVersion7();


    private const string View = "users.view";
    private const string Create = "users.create";

    private static AuthorizationSnapshot Snapshot(params GrantedPermission[] grants) =>
        AuthorizationSnapshot.FromGrants(Person, grants);

    [Fact]
    public void Empty_Snapshot_Denies_Everything()
    {
        var snapshot = AuthorizationSnapshot.Empty(Person);

        Assert.True(snapshot.IsEmpty);
        Assert.False(snapshot.Has(View, BranchScope.At(Makati)));
        Assert.False(snapshot.Has(View, BranchScope.OrgWide));
    }

    [Fact]
    public void An_OrgWide_Grant_Covers_A_Branch_Request()
    {
        var snapshot = Snapshot(new GrantedPermission(View, null));

        Assert.True(snapshot.Has(View, BranchScope.At(Makati)));
        Assert.True(snapshot.Has(View, BranchScope.OrgWide));
        Assert.False(snapshot.IsEmpty);
    }


    [Fact]
    public void A_Branch_Grant_Does_Not_Cover_An_OrgWide_Request()
    {
        var snapshot = Snapshot(new GrantedPermission(View, Makati));

        Assert.True(snapshot.Has(View, BranchScope.At(Makati)));
        Assert.False(snapshot.Has(View, BranchScope.OrgWide));
    }

    [Fact]
    public void A_Branch_Grant_Does_Not_Cover_Another_Branch()
    {
        var snapshot = Snapshot(new GrantedPermission(View, Makati));

        Assert.False(snapshot.Has(View, BranchScope.At(Cebu)));
    }

    
    [Fact]
    public void Grants_From_Two_Roles_Union()
    {
        var snapshot = Snapshot(
            new GrantedPermission(View, Makati),
            new GrantedPermission(View, Cebu));

        Assert.True(snapshot.Has(View, BranchScope.At(Makati)));
        Assert.True(snapshot.Has(View, BranchScope.At(Cebu)));
        Assert.False(snapshot.Has(View, BranchScope.OrgWide));
    }

    [Fact]
    public void Duplicate_Grants_Are_Collapsed()
    {
        var snapshot = Snapshot(
            new GrantedPermission(View, Makati),
            new GrantedPermission(View, Makati));


        var reach = (Dictionary<string, HashSet<BranchScope>>)typeof(AuthorizationSnapshot)
            .GetField("_reach", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(snapshot)!;

        Assert.Single(reach[View]);
    }

    [Fact]
    public void Has_Throws_On_A_Null_Scope()
    {
        var snapshot = Snapshot(new GrantedPermission(View, null));

       
        Assert.Throws<ArgumentNullException>(() => snapshot.Has(View, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Has_Rejects_A_Blank_Key(string key)
    {
        var snapshot = Snapshot(new GrantedPermission(View, null));

        Assert.Throws<ArgumentException>(() => snapshot.Has(key, BranchScope.OrgWide));
    }

    
    [Fact]
    public void An_Uncatalogued_Key_Is_Denied()
    {
        var snapshot = Snapshot(new GrantedPermission(View, null));

        Assert.False(snapshot.Has("expenses.approve", BranchScope.OrgWide));
        Assert.False(snapshot.Has("Users.View", BranchScope.OrgWide));
    }

    // ---------------- guard tests ----------------

    [Fact]
    public void Snapshot_Exposes_No_Branch_Blind_Question()
    {
        var type = typeof(AuthorizationSnapshot);
        const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance
                                  | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var method in type.GetMethods(Public).Where(m => m.Name == nameof(AuthorizationSnapshot.Has)))
        {
            var parameters = method.GetParameters();

            Assert.Equal(2, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal(typeof(BranchScope), parameters[1].ParameterType);
        }


        var leaks = type.GetProperties(Public)
            .Select(p => (p.Name, Type: p.PropertyType))
            .Concat(type.GetMethods(Public).Select(m => (m.Name, Type: m.ReturnType)))
            .Where(member => member.Type != typeof(string)
                          && typeof(IEnumerable<string>).IsAssignableFrom(member.Type))
            .Select(member => member.Name)
            .ToList();

        Assert.Empty(leaks);
    }

    [Fact]
    public void Snapshot_Holds_No_Entities()
    {
        var offenders = typeof(AuthorizationSnapshot)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(field => MentionsEntities(field.FieldType))
            .Select(field => field.Name)
            .ToList();

        Assert.Empty(offenders);
    }

    private static bool MentionsEntities(Type type) =>
        type.Namespace == "SAFC.Expense.Domain.Entities"
        || (type.IsGenericType && type.GetGenericArguments().Any(MentionsEntities));
}

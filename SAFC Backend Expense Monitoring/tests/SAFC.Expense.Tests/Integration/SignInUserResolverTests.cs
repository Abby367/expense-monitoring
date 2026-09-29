using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SAFC.Expense.Domain.Entities;
using SAFC.Expense.Domain.Enums;
using SAFC.Expense.Infrastructure.Identity;
using SAFC.Expense.Infrastructure.Persistence;

namespace SAFC.Expense.Tests.Integration;

[Collection("Integration")]
public sealed class SignInUserResolverTests(IntegrationDatabase database)
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.CreateVersion7();

    private const string Oid = "2f1a9c4e-8b7d-4a3f-9c21-5e6d7f8a9b0c";
    private const string Email = "probe@safc.com.ph";

    /// <summary>
    /// Unlike the resolver tests next door, these WRITE — RecordMicrosoftSignIn commits.
    /// The transaction is never committed, so the rollback is doing real work here.
    /// </summary>
    private async Task RunAsync(Func<ExpenseDbContext, SignInUserResolver, Task> body)
    {
        await using var context = database.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        await body(context, new SignInUserResolver(
            context, NullLogger<SignInUserResolver>.Instance));
    }

    [Fact]
    public async Task Finds_The_User_By_Microsoft_Id()
    {
        await RunAsync(async (context, resolver) =>
        {
            var user = User.CreateByAdminWithMicrosoft(Email, "Probe", Actor, Now);
            user.RecordMicrosoftSignIn(Oid, Now);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // email: null on purpose — this proves the fast path found it, not the fallback.
            var id = await resolver.ResolveAsync(Oid, email: null, CancellationToken.None);

            Assert.Equal(user.Id, id);
        });
    }

    [Fact]
    public async Task Finds_By_Email_When_Microsoft_Id_Is_Null_And_Pins_It()
    {
        await RunAsync(async (context, resolver) =>
        {
            var user = User.CreateByAdminWithMicrosoft(Email, "Probe", Actor, Now);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // The state every row in the real database is in today.
            Assert.Null(user.MicrosoftId);

            var id = await resolver.ResolveAsync(Oid, Email, CancellationToken.None);

            Assert.Equal(user.Id, id);

            // Pinned AND committed — read it back, or this passes on the change tracker alone.
            var reloaded = await context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);

            Assert.Equal(Oid, reloaded.MicrosoftId);
            Assert.Equal(UserStatus.Active, reloaded.Status);
        });
    }

    [Fact]
    public async Task An_Unknown_Email_Is_Refused_And_No_User_Is_Created()
    {
        await RunAsync(async (context, resolver) =>
        {
            var before = await context.Users.CountAsync();

            var id = await resolver.ResolveAsync(
                Oid, "stranger@safc.com.ph", CancellationToken.None);

            Assert.Null(id);

            // D23. "Returns null" alone would still pass if a row were created and not returned.
            Assert.Equal(before, await context.Users.CountAsync());
        });
    }

    [Fact]
    public async Task A_Removed_User_Cannot_Sign_In()
    {
        await RunAsync(async (context, resolver) =>
        {
            var user = User.CreateByAdminWithMicrosoft(Email, "Probe", Actor, Now);
            user.Remove(Actor, "Left the company.", Now);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var id = await resolver.ResolveAsync(Oid, Email, CancellationToken.None);

            Assert.Null(id);
        });
    }

    [Fact]
    public async Task An_Email_Linked_To_A_Different_Microsoft_Id_Is_Refused()
    {
        await RunAsync(async (context, resolver) =>
        {
            var user = User.CreateByAdminWithMicrosoft(Email, "Probe", Actor, Now);
            user.RecordMicrosoftSignIn(Oid, Now);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var id = await resolver.ResolveAsync(
                Guid.CreateVersion7().ToString(), Email, CancellationToken.None);

            Assert.Null(id);
        });
    }
    [Fact]
    public async Task A_Non_SAFC_Email_Still_Resolves()
    {
        await RunAsync(async (context, resolver) =>
        {
            const string external = "contractor@example.com";

            var user = User.CreateByAdminWithMicrosoft(external, "Contractor", Actor, Now);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var id = await resolver.ResolveAsync(Oid, external, CancellationToken.None);

            Assert.Equal(user.Id, id);
        });
    }
    [Fact]
    public async Task A_Removed_User_With_A_Pinned_Oid_Cannot_Sign_In()
    {
        await RunAsync(async (context, resolver) =>
        {
            var user = User.CreateByAdminWithMicrosoft(Email, "Probe", Actor, Now);
            user.RecordMicrosoftSignIn(Oid, Now);
            user.Remove(Actor, "Left the company.", Now);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var id = await resolver.ResolveAsync(Oid, email: null, CancellationToken.None);

            Assert.Null(id);
        });
    }

    [Fact]
    public async Task The_Oid_Is_Matched_Case_Insensitively()
    {
        await RunAsync(async (context, resolver) =>
        {
            var user = User.CreateByAdminWithMicrosoft(Email, "Probe", Actor, Now);
            user.RecordMicrosoftSignIn(Oid, Now);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var id = await resolver.ResolveAsync(
                Oid.ToUpperInvariant(), email: null, CancellationToken.None);

            Assert.Equal(user.Id, id);
        });
    }


}

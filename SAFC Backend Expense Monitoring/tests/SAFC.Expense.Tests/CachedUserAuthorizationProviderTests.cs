using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SAFC.Expense.Application.Common.Authorization;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Infrastructure.Identity;

namespace SAFC.Expense.Tests;

public class CachedUserAuthorizationProviderTests
{

    private sealed class CountingProvider : IUserAuthorizationProvider
    {
        public int Calls { get; private set; }

        public Task<AuthorizationSnapshot> GetSnapshotAsync(
            Guid userId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(AuthorizationSnapshot.Empty(userId));
        }
    }

    private static CachedUserAuthorizationProvider Build(IUserAuthorizationProvider inner, int seconds) =>
        new(inner,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new AuthorizationCacheOptions { SnapshotSeconds = seconds }));

    [Fact]
    public async Task A_Second_Call_Inside_The_Window_Does_Not_Reach_The_Database()
    {
        var inner = new CountingProvider();
        var cached = Build(inner, 10);
        var userId = Guid.CreateVersion7();

        await cached.GetSnapshotAsync(userId);
        await cached.GetSnapshotAsync(userId);

        Assert.Equal(1, inner.Calls);
    }


    [Fact]
    public async Task An_Empty_Snapshot_Is_Cached_Too()
    {
        var inner = new CountingProvider();
        var cached = Build(inner, 10);
        var userId = Guid.CreateVersion7();

        var first = await cached.GetSnapshotAsync(userId);
        await cached.GetSnapshotAsync(userId);

        Assert.True(first.IsEmpty);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Different_Users_Do_Not_Share_An_Entry()
    {
        var inner = new CountingProvider();
        var cached = Build(inner, 10);

        await cached.GetSnapshotAsync(Guid.CreateVersion7());
        await cached.GetSnapshotAsync(Guid.CreateVersion7());

        Assert.Equal(2, inner.Calls);
    }


    [Fact]
    public async Task Zero_Seconds_Disables_The_Cache_Without_Throwing()
    {
        var inner = new CountingProvider();
        var cached = Build(inner, 0);
        var userId = Guid.CreateVersion7();

        await cached.GetSnapshotAsync(userId);
        await cached.GetSnapshotAsync(userId);

        Assert.Equal(2, inner.Calls);
    }
}

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SAFC.Expense.Application.Common.Authorization;
using SAFC.Expense.Application.Common.Interfaces;

namespace SAFC.Expense.Infrastructure.Identity;

/// <summary>
/// Caches resolved snapshots for a short, absolute window.
/// </summary>
/// <remarks>
/// A separate class from the query on purpose: the query is testable without a cache and the cache
/// is testable without a database. If this ever runs on more than one instance, only this file
/// changes — IMemoryCache becomes HybridCache or Redis.
///
/// Eviction on grant/revoke is an optimisation, NOT the mechanism. IMemoryCache is process-local,
/// so evicting on instance A does nothing for instance B — the TTL is therefore chosen as if
/// eviction did not exist.
/// </remarks>
public sealed class CachedUserAuthorizationProvider(
    IUserAuthorizationProvider inner,
    IMemoryCache cache,
    IOptions<AuthorizationCacheOptions> options) : IUserAuthorizationProvider
{
    public async Task<AuthorizationSnapshot> GetSnapshotAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var seconds = options.Value.SnapshotSeconds;


        if (seconds <= 0)
            return await inner.GetSnapshotAsync(userId, cancellationToken);


        var key = $"authz:v1:{userId:D}";

        if (cache.TryGetValue(key, out AuthorizationSnapshot? cached) && cached is not null)
            return cached;

        var snapshot = await inner.GetSnapshotAsync(userId, cancellationToken);


        cache.Set(key, snapshot, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds)
        });

        return snapshot;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFC.Expense.Application.Common.Interfaces;
using SAFC.Expense.Domain.Entities;

namespace SAFC.Expense.Infrastructure.Identity;

internal sealed class SignInUserResolver(
    IExpenseDbContext context,
    ILogger<SignInUserResolver> logger)
{
    /// <summary>
    /// Finds the user this Microsoft identity belongs to and records the first sign-in.
    /// NEVER creates a user (D23). Returns null for every refusal.
    /// </summary>
    internal async Task<Guid?> ResolveAsync(
        string microsoftObjectId, string? email, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // A blank oid is a refusal, not a fallback. Entra's `sub` is a pairwise identifier —
        // a well-formed Guid that matches no row, forever — so the defensive-looking `??`
        // in onboarding's oid → sub → NameIdentifier chain is the bug, not the safeguard.
        if (string.IsNullOrWhiteSpace(microsoftObjectId))
            return null;

        var incoming = User.NormalizeMicrosoftId(microsoftObjectId);

        // No IgnoreQueryFilters anywhere in this class: a soft-removed user must not sign in.
        // The inverse of the seeder's reads — filters off when reconciling a seed list,
        // filters on when deciding authority.
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.MicrosoftId == incoming, cancellationToken);

        if (user is null && !string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = User.NormalizeEmail(email);

            user = await context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        }

        // D23: no row, no sign-in. This method never creates one.
        if (user is null)
        {
            logger.LogWarning(
                "Sign-in refused: no user row for {Email} (oid {IncomingId}). "
                + "The account is valid in Entra but was never invited to this application.",
                email, incoming);

            return null;
        }

        if (user.MicrosoftId is not null && user.MicrosoftId != incoming)
        {
            logger.LogWarning(
                "Sign-in refused: {Email} is linked to {StoredId} but presented {IncomingId}.",
                user.Email, user.MicrosoftId, incoming);

            return null;
        }

        // Logged, never refused. The tenant pin already decided who may hold a token at all,
        // so a domain gate on top of it locks out legitimate guests and buys nothing.
        if (!user.Email.EndsWith("@safc.com.ph", StringComparison.OrdinalIgnoreCase))
            logger.LogWarning(
                "Sign-in by {Email}, which is not an @safc.com.ph address.", user.Email);

        try
        {
            // The bool exists so an ordinary sign-in writes nothing. The commit must land
            // before anything resolves a snapshot, or D22's 10-second negative cache denies
            // this person on their very first sign-in.
            if (user.RecordMicrosoftSignIn(incoming, now))
                await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // DbUpdateException and nothing wider. "The database is down" must not masquerade
            // as "you are not authorised" — that is a 500 or a 503, and it should stay one.
            logger.LogWarning(
                exception, "Sign-in could not record a first sign-in for {Email}.", user.Email);

            return null;
        }

        return user.Id;
    }
}

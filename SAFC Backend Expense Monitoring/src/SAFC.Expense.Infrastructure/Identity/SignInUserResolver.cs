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
        if (string.IsNullOrWhiteSpace(microsoftObjectId))
            return null;


        var incoming = User.NormalizeMicrosoftId(microsoftObjectId);

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.MicrosoftId == incoming, cancellationToken);

        if (user is null && !string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = User.NormalizeEmail(email);

            user = await context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        }

        if (user is null)
            return null;


        if (user.MicrosoftId is not null && user.MicrosoftId != incoming)
        {
            logger.LogWarning(
                "Sign-in refused: {Email} is linked to {StoredId} but presented {IncomingId}.",
                user.Email, user.MicrosoftId, incoming);

            return null;
        }

        try
        {

            if (user.RecordMicrosoftSignIn(incoming, now))
                await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {

            logger.LogWarning(
                exception, "Sign-in could not record a first sign-in for {Email}.", user.Email);

            return null;
        }

        return user.Id;
    }
}

using SAFC.Expense.Application.Common.Authorization;

namespace SAFC.Expense.Application.Common.Interfaces;

public interface IUserAuthorizationProvider
{

    Task<AuthorizationSnapshot> GetSnapshotAsync(
        Guid userId, CancellationToken cancellationToken = default);
}

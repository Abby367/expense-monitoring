using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Infrastructure.Persistence;

namespace SAFC.Expense.Tests.Integration;


public sealed class IntegrationDatabase : IAsyncLifetime
{
    public const string ConnectionVariable = "SAFC_EXPENSE_TEST_CONNECTION";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable);

      
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                $"{ConnectionVariable} is not set. These tests need a dedicated PostgreSQL " +
                "database. There is deliberately no default.");

        if (connection.Contains("safc_expense_v2", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"{ConnectionVariable} points at the development database. Use a dedicated one.");

        ConnectionString = connection;

       
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public ExpenseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ExpenseDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);
}

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationDatabase>;

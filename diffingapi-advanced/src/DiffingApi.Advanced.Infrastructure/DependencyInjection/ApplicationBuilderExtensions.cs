using DiffingApi.Advanced.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DiffingApi.Advanced.Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static IServiceProvider InitializeInfrastructure(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DiffDbContext>();

        var created = dbContext.Database.EnsureCreated();

        if (!created && !TableExists(dbContext, "DiffPairs"))
        {
            var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
            databaseCreator.CreateTables();
        }

        return services;
    }

    private static bool TableExists(DiffDbContext dbContext, string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table' AND name = $name
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }
}

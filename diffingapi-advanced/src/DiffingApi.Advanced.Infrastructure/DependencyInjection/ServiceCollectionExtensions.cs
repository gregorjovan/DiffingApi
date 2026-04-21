using DiffingApi.Advanced.Application.Abstractions;
using DiffingApi.Advanced.Infrastructure.Persistence;
using DiffingApi.Advanced.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiffingApi.Advanced.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = BuildConnectionString(
            configuration.GetConnectionString("DiffingDatabase"),
            contentRootPath);

        services.AddDbContextFactory<DiffingAdvancedDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton<IDiffContentStore, SqliteDiffContentStore>();

        return services;
    }

    private static string BuildConnectionString(string? configuredConnectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(
            string.IsNullOrWhiteSpace(configuredConnectionString)
                ? "Data Source=data/diffingapi-advanced.db"
                : configuredConnectionString);

        var databasePath = builder.DataSource;

        if (string.IsNullOrWhiteSpace(databasePath))
        {
            databasePath = "data/diffingapi-advanced.db";
        }

        if (!Path.IsPathRooted(databasePath))
        {
            databasePath = Path.Combine(contentRootPath, databasePath);
        }

        var directory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        builder.DataSource = databasePath;

        return builder.ConnectionString;
    }
}

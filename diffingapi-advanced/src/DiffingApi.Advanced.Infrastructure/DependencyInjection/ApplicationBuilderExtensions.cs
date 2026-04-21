using DiffingApi.Advanced.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiffingApi.Advanced.Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static IServiceProvider InitializeInfrastructure(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DiffingAdvancedDbContext>>();
        using var dbContext = dbContextFactory.CreateDbContext();

        dbContext.Database.EnsureCreated();

        return services;
    }
}

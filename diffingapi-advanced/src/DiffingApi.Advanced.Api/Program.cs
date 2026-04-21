using DiffingApi.Advanced.Api.Endpoints;
using DiffingApi.Advanced.Application.Abstractions;
using DiffingApi.Advanced.Application.Services;
using DiffingApi.Advanced.Infrastructure;
using DiffingApi.Advanced.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IDiffLockProvider, DiffLockProvider>();
builder.Services.AddScoped<IDiffPairRepository, SqliteDiffPairRepository>();
builder.Services.AddScoped<IDiffService, DiffService>();

var app = builder.Build();


app.Services.InitializeInfrastructure();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "Diffing API");
    });
}

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapApplicationEndpoints();

app.Run();

public partial class Program;

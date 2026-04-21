using DiffingApi.Advanced.Api.Endpoints;
using DiffingApi.Advanced.Application.Abstractions;
using DiffingApi.Advanced.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IDiffContentStore, InMemoryDiffContentStore>();

var app = builder.Build();

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

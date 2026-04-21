using DiffingApi.Basic.Services;
using DiffingApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DiffContentStore>();

builder.Services.AddOpenApi();

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
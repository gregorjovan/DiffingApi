using DiffingApi.Endpoints;
using DiffingApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DiffContentStore>();

var app = builder.Build();
app.MapApplicationEndpoints();

app.Run();

public partial class Program;

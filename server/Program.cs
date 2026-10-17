using Game.Server.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, log) => log.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

// A10: no player state in memory; everything goes through the database.
builder.Services.AddDbContext<GameDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Game")));
builder.Services.AddHealthChecks().AddDbContextCheck<GameDbContext>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSerilogRequestLogging();

// D-62: OpenAPI + Scalar UI in dev only.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory in tests.
public partial class Program { }

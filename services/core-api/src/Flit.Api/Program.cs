using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Flit.Infrastructure;
using Flit.Infrastructure.Persistence;
using Flit.SharedKernel;

// FLIT 2.0 — esqueleto base (post-reset). Sin modulos de negocio.
// Superficie API minima: health. Las nuevas features se montan sobre esta base.

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "core-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddCors(opts => opts.AddDefaultPolicy(p => p
    .WithOrigins(
        builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:4001")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddHttpClient();

// Persistencia: se registra solo si hay ConnectionStrings:Core configurada.
// El DbContext arranca vacio; las nuevas features agregan sus DbSets/migraciones.
var coreConnStr = builder.Configuration.GetConnectionString("Core");
var usePostgres = !string.IsNullOrEmpty(coreConnStr);
if (usePostgres)
{
    builder.Services.AddPostgresInfrastructure(coreConnStr!);
}

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

// ─── EF Core auto-migrate — solo si hay base configurada ─────────
if (usePostgres)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<FlitDbContext>();
    await db.Database.MigrateAsync();
    Log.Information(
        "✓ Migraciones EF Core aplicadas correctamente ({Environment})",
        app.Environment.EnvironmentName);
}

app.UseCors();

app.MapGet("/api/v1/health", () => new HealthResponse(
    Status: "ok",
    Service: "core-api",
    Version: "2.0.0-flit-shell"
))
.WithName("Health")
.WithTags("System");

app.MapGet("/", () => Results.Redirect("/api/v1/health"));

app.Run();

internal sealed record HealthResponse(string Status, string Service, string Version);

public partial class Program;

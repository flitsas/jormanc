using System.Text.Json.Serialization;
using Flit.Api.Endpoints;
using Flit.Api.HostedServices;
using Flit.Api.Hubs;
using Flit.Api.Infrastructure;
using Flit.Api.Middleware;
using Flit.Modules.Procedures.Domain.Interfaces;
using Flit.Infrastructure;
using Flit.Infrastructure.Persistence;
using Flit.Modules.Companies;
using Flit.Modules.Integrations;
using Flit.Modules.Identity;
using Flit.Modules.Documents;
using Flit.Modules.Procedures;
using Flit.Modules.ProceduresConfig;
using Flit.Modules.Analytics;
using Flit.Modules.OT;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.Modules.Identity.Infrastructure.Security;
using Flit.SharedKernel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

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

// ─── IMemoryCache para ISessionBlacklist (ADR-0013) ──────────────────────────
builder.Services.AddMemoryCache();

// ─── SignalR — notificaciones de sesión en tiempo real (ADR-0013, HU-9771) ───
builder.Services.AddSignalR();
builder.Services.AddScoped<ISessionNotifier, SignalRSessionNotifier>();
builder.Services.AddScoped<IProcedureStatusNotifier, SignalRProcedureStatusNotifier>();

// ─── Persistencia ────────────────────────────────────────────────────────────
var coreConnStr = builder.Configuration.GetConnectionString("Core");
var usePostgres = !string.IsNullOrEmpty(coreConnStr);
if (usePostgres)
{
    builder.Services.AddPostgresInfrastructure(coreConnStr!);
}

// ─── JWT RS256: construir RsaKeyProvider antes del service provider ──────────
var jwtSection = builder.Configuration.GetSection(IdentityJwtOptions.SectionName);
var jwtOptions = jwtSection.Get<IdentityJwtOptions>() ?? new IdentityJwtOptions();
var rsaKeyProvider = new RsaKeyProvider(jwtOptions, builder.Environment.ContentRootPath);

// ─── Módulo Identity ─────────────────────────────────────────────────────────
builder.Services.AddIdentityModule(builder.Configuration, rsaKeyProvider);

// ─── Módulo Companies (HU-9774) ───────────────────────────────────────────────
builder.Services.AddCompaniesModule();

// ─── Módulo Integrations (HU-9775) — Strategy + ConnectorRouter RUNT ─────────
builder.Services.AddIntegrationsModule(builder.Configuration);

// ─── Módulo ProceduresConfig (HU-9779) — Parametrizador low-code ────────────
builder.Services.AddProceduresConfigModule();

// ─── Módulo Procedures (HU-9784+) — Creación de trámites runtime ──────────────
builder.Services.AddProceduresModule();

// ─── Módulo Documents (HU-9789) — Maestro documental ─────────────────────────
builder.Services.AddDocumentsModule();

// ─── Módulo Analytics (HU-9794) — Dashboard KPIs ─────────────────────────────
builder.Services.AddAnalyticsModule();

// ─── Módulo OT (HU-9798) — CRUD Organismos de Tránsito ───────────────────────
builder.Services.AddOtModule();

// ─── ProcedureSubmitted: Procedures stub + Documents pipeline (HU-9791) ───────
builder.Services.AddScoped<IProcedureEventPublisher, CompositeProcedureEventPublisher>();

// ─── JWT Authentication (valida tokens en Flit.Api, p.ej. /auth/me) ──────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        // Preservar nombres JWT estándar (sub, tid) para endpoints que usan JwtRegisteredClaimNames.
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwtOptions.Issuer),
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = !string.IsNullOrEmpty(jwtOptions.Audience),
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaKeyProvider.SigningKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Allow SignalR to send JWT via query-string (WebSocket transport)
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ─── Blacklist middleware (registrar como IMiddleware para inyección DI) ──────
builder.Services.AddTransient<JwtBlacklistMiddleware>();

// ─── Hosted services ─────────────────────────────────────────────────────────
if (usePostgres)
{
    builder.Services.AddHostedService<BlacklistRehydrationService>();
    builder.Services.AddHostedService<DevSeedService>();
}

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

// ─── EF Core auto-migrate ─────────────────────────────────────────────────────
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
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<JwtBlacklistMiddleware>();

// ─── Endpoints ───────────────────────────────────────────────────────────────
app.MapGet("/api/v1/health", () => new HealthResponse(
    Status: "ok",
    Service: "core-api",
    Version: "2.0.0-flit-shell"
))
.WithName("Health")
.WithTags("System");

app.MapGet("/", () => Results.Redirect("/api/v1/health"));

app.MapAuthEndpoints();
app.MapRolesEndpoints();
app.MapInvitationEndpoints();
app.MapCompaniesEndpoints();
app.MapIntegrationEndpoints();
app.MapProcedureTypesEndpoints();
app.MapProceduresEndpoints();
app.MapDocumentsEndpoints();
app.MapDashboardEndpoints();
app.MapOtOrganismsEndpoints();
app.MapOtWebhooksEndpoints();

// ─── SignalR Hubs ─────────────────────────────────────────────────────────────
app.MapHub<SessionHub>("/hubs/session");

app.Run();

internal sealed record HealthResponse(string Status, string Service, string Version);

public partial class Program;

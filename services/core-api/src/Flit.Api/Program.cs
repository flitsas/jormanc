using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Flit.Api.Endpoints;
using Flit.Infrastructure;
using Flit.Infrastructure.Persistence;
using Flit.Modules.Identity.Adapters;
using Flit.Modules.Identity.Ports;
using Flit.SharedKernel;

// FLIT 2.0 — base limpia post-reset trámites (2026-06).
// Superficie API: health, users, RBAC, auth, menú/permisos del usuario actual.

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

builder.Services.AddSingleton<IUsuariosRepository, InMemoryUsuariosRepository>();
builder.Services.AddSingleton<ICredentialsRepository, InMemoryCredentialsRepository>();
builder.Services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();

var coreConnStr = builder.Configuration.GetConnectionString("Core");
var usePostgres = !string.IsNullOrEmpty(coreConnStr);
if (usePostgres)
{
    builder.Services.AddPostgresInfrastructure(coreConnStr!);
}

builder.Services.AddSingleton<Flit.Modules.Users.Ports.ICognitoDirectory,
    Flit.Modules.Users.Adapters.StubCognitoDirectory>();
builder.Services.AddSingleton<Flit.Modules.Users.Application.IPasswordGenerator,
    Flit.Modules.Users.Adapters.TempPasswordGenerator>();
if (!usePostgres)
{
    builder.Services.AddSingleton<Flit.Modules.Users.Ports.IUsersRepository,
        Flit.Modules.Users.Adapters.InMemoryUsersRepository>();
    builder.Services.AddSingleton<Flit.Modules.Users.Application.IUnitOfWork,
        Flit.Modules.Users.Adapters.InMemoryUnitOfWork>();
}

builder.Services.AddSingleton<Flit.Modules.Auth.Domain.Aes256GcmSecretCipher>(sp =>
{
    var keyB64 = builder.Configuration["Mfa:MasterKeyBase64"];
    var keyId = builder.Configuration["Mfa:KeyId"] ?? "v1";
    if (!string.IsNullOrWhiteSpace(keyB64))
        return Flit.Modules.Auth.Domain.Aes256GcmSecretCipher.FromEnv(keyB64, keyId);
    var ephemeral = new byte[32];
    System.Security.Cryptography.RandomNumberGenerator.Fill(ephemeral);
    Log.Warning("⚠  MFA_MASTER_KEY_BASE64 no configurada — usando key efimera (DEV ONLY)");
    return new Flit.Modules.Auth.Domain.Aes256GcmSecretCipher(ephemeral, $"{keyId}-ephemeral");
});
builder.Services.AddSingleton<Flit.Modules.Auth.Domain.TotpService>();
builder.Services.AddSingleton<Flit.Modules.Auth.Ports.IMfaSessionStore,
    Flit.Modules.Auth.Adapters.InMemoryMfaSessionStore>();
if (!usePostgres)
{
    builder.Services.AddSingleton<Flit.Modules.Auth.Application.IPasswordResetTokensRepository,
        Flit.Modules.Auth.Adapters.InMemoryPasswordResetTokensRepository>();
}

builder.Services.AddSingleton<Flit.Modules.Rbac.Ports.IPermissionsCache,
    Flit.Modules.Rbac.Adapters.InMemoryPermissionsCache>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<Flit.Modules.ProceduresConfig.Application.EndpointInvocationRateLimiter>();
builder.Services.AddSingleton<Flit.Modules.ProceduresConfig.Adapters.InMemoryProcedureRulesRepository>();
builder.Services.AddSingleton<Flit.Modules.ProceduresConfig.Adapters.InMemoryEndpointCatalogRepository>();
builder.Services.AddSingleton<Flit.Modules.ProceduresConfig.Adapters.InMemoryEndpointCallLogRepository>();
if (!usePostgres)
{
    builder.Services.AddSingleton<Flit.Modules.ProceduresConfig.Ports.IProcedureRulesRepository>(sp =>
        sp.GetRequiredService<Flit.Modules.ProceduresConfig.Adapters.InMemoryProcedureRulesRepository>());
    builder.Services.AddSingleton<Flit.Modules.ProceduresConfig.Ports.IEndpointCatalogRepository>(sp =>
        sp.GetRequiredService<Flit.Modules.ProceduresConfig.Adapters.InMemoryEndpointCatalogRepository>());
    builder.Services.AddSingleton<Flit.Modules.ProceduresConfig.Ports.IEndpointCallLogRepository>(sp =>
        sp.GetRequiredService<Flit.Modules.ProceduresConfig.Adapters.InMemoryEndpointCallLogRepository>());
    builder.Services.AddSingleton<Flit.Modules.ProceduresConfig.Ports.IRuleEndpointInvoker>(sp =>
        new Flit.Modules.ProceduresConfig.Application.CatalogRuleEndpointInvoker(
            sp.GetRequiredService<Flit.Modules.ProceduresConfig.Ports.IEndpointCatalogRepository>(),
            sp.GetRequiredService<Flit.Modules.ProceduresConfig.Ports.IEndpointCallLogRepository>(),
            sp.GetRequiredService<Flit.Modules.ProceduresConfig.Application.EndpointInvocationRateLimiter>(),
            sp.GetRequiredService<IHttpClientFactory>()));

    builder.Services.AddSingleton<Flit.Modules.Rbac.Adapters.InMemoryRoleAssignmentsRepository>();
    builder.Services.AddSingleton<Flit.Modules.Rbac.Ports.IRoleAssignmentsRepository>(
        sp => sp.GetRequiredService<Flit.Modules.Rbac.Adapters.InMemoryRoleAssignmentsRepository>());
    builder.Services.AddSingleton<Flit.Modules.Rbac.Ports.IRolesRepository,
        Flit.Modules.Rbac.Adapters.InMemoryRolesRepository>();
    builder.Services.AddSingleton<Flit.Modules.Rbac.Ports.IPermissionsRepository,
        Flit.Modules.Rbac.Adapters.InMemoryPermissionsRepository>();
    builder.Services.AddSingleton<Flit.Modules.Rbac.Ports.IMenuItemsRepository,
        Flit.Modules.Rbac.Adapters.InMemoryMenuItemsRepository>();
}

builder.Services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
var identityProviderName = builder.Configuration["Identity:Provider"] ?? "Local";
if (string.Equals(identityProviderName, "Cognito", StringComparison.OrdinalIgnoreCase))
{
    var cognitoSettings = new CognitoSettings(
        UserPoolId: builder.Configuration["Cognito:UserPoolId"]
            ?? throw new InvalidOperationException("Cognito:UserPoolId requerido cuando Identity:Provider=Cognito"),
        AppClientId: builder.Configuration["Cognito:AppClientId"]
            ?? throw new InvalidOperationException("Cognito:AppClientId requerido"),
        AppClientSecret: builder.Configuration["Cognito:AppClientSecret"] ?? string.Empty);
    builder.Services.AddSingleton(cognitoSettings);
    builder.Services.AddSingleton<Amazon.CognitoIdentityProvider.IAmazonCognitoIdentityProvider>(sp =>
    {
        var region = builder.Configuration["AWS:Region"] ?? "us-east-1";
        var accessKey = builder.Configuration["AWS:AccessKeyId"];
        var secretKey = builder.Configuration["AWS:SecretAccessKey"];
        var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
        return !string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey)
            ? new Amazon.CognitoIdentityProvider.AmazonCognitoIdentityProviderClient(
                accessKey, secretKey, regionEndpoint)
            : new Amazon.CognitoIdentityProvider.AmazonCognitoIdentityProviderClient(regionEndpoint);
    });
    builder.Services.AddSingleton<IIdentityProvider, CognitoIdentityProvider>();
}
else
{
    builder.Services.AddScoped<IIdentityProvider, LocalIdentityProvider>();
}

builder.Services.AddSingleton<ITokenIssuer>(sp =>
{
    var clock = sp.GetRequiredService<IClock>();
    var config = sp.GetRequiredService<IConfiguration>();
    var contentRoot = sp.GetRequiredService<IHostEnvironment>().ContentRootPath;
    var settings = LoadJwtSettings(config, contentRoot);
    return new RsaJwtTokenIssuer(settings, clock);
});

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

// ─── EF Core auto-migrate — Dev, QA y PDN ────────────────────────
if (!string.IsNullOrEmpty(coreConnStr))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<FlitDbContext>();
    await db.Database.MigrateAsync();
    Log.Information(
        "✓ Migraciones EF Core aplicadas correctamente ({Environment})",
        app.Environment.EnvironmentName);
}
else if (!app.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "ConnectionStrings:Core es requerida. Las migraciones EF Core deben aplicarse al arrancar en Dev, QA y PDN.");
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

app.MapUsersEndpoints();
app.MapRbacEndpoints();
app.MapProceduresConfigEndpoints();
app.MapAuthEndpoints();
app.MapDevSeedEndpoints(app.Environment);

app.MapGet("/api/v1/menu/me",
    async (Guid userId, Flit.Modules.Rbac.Ports.IMenuItemsRepository menuRepo, CancellationToken ct) =>
{
    var response = await Flit.Modules.Rbac.Application.GetUserMenu.HandleAsync(
        new Flit.Modules.Rbac.Application.GetUserMenu.Query(userId), menuRepo, ct);
    return Results.Ok(response);
})
.WithName("GetCurrentUserMenu")
.WithTags("RBAC - Current User");

app.MapGet("/api/v1/permissions/me",
    async (Guid userId,
        Flit.Modules.Rbac.Ports.IPermissionsRepository permsRepo,
        Flit.Modules.Rbac.Ports.IPermissionsCache cache,
        CancellationToken ct) =>
{
    var response = await Flit.Modules.Rbac.Application.GetUserPermissions.HandleAsync(
        new Flit.Modules.Rbac.Application.GetUserPermissions.Query(userId), permsRepo, cache, ct);
    return Results.Ok(response);
})
.WithName("GetCurrentUserPermissions")
.WithTags("RBAC - Current User");

app.Run();

static bool TryReadJwtKeyFiles(
    string contentRoot,
    string? privateKeyPath,
    string? publicKeyPath,
    out string privatePem,
    out string publicPem)
{
    privatePem = string.Empty;
    publicPem = string.Empty;
    if (string.IsNullOrWhiteSpace(privateKeyPath) || string.IsNullOrWhiteSpace(publicKeyPath))
        return false;

    var privFull = Path.IsPathRooted(privateKeyPath)
        ? privateKeyPath
        : Path.GetFullPath(Path.Combine(contentRoot, privateKeyPath));
    var pubFull = Path.IsPathRooted(publicKeyPath)
        ? publicKeyPath
        : Path.GetFullPath(Path.Combine(contentRoot, publicKeyPath));

    if (!File.Exists(privFull) || !File.Exists(pubFull))
        return false;

    privatePem = File.ReadAllText(privFull);
    publicPem = File.ReadAllText(pubFull);
    return true;
}

static JwtSettings LoadJwtSettings(IConfiguration config, string contentRoot)
{
    var issuer = config["Jwt:Issuer"] ?? "tramites-core";
    var audience = config["Jwt:Audience"] ?? "tramites-internal";
    var accessTtlMin = int.TryParse(config["Jwt:AccessTokenMinutes"], out var aTtl) ? aTtl : 15;
    var refreshTtlDays = int.TryParse(config["Jwt:RefreshTokenDays"], out var rTtl) ? rTtl : 7;
    var devGenerate = string.Equals(
        config["Jwt:DevGenerate"], "true", StringComparison.OrdinalIgnoreCase);

    string privatePem, publicPem;

    if (devGenerate)
    {
        var privPath = config["Jwt:PrivateKeyPath"];
        var pubPath = config["Jwt:PublicKeyPath"];
        if (TryReadJwtKeyFiles(contentRoot, privPath, pubPath, out privatePem, out publicPem))
            return new JwtSettings(
                PrivateKeyPem: privatePem,
                PublicKeyPem: publicPem,
                Issuer: issuer,
                Audience: audience,
                AccessTtl: TimeSpan.FromMinutes(accessTtlMin),
                RefreshTtl: TimeSpan.FromDays(refreshTtlDays));

        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        privatePem = rsa.ExportRSAPrivateKeyPem();
        publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        Log.Warning(
            "JWT DevGenerate: llaves efimeras (DEV). Ejecuta ./scripts/gen-secrets.sh para llaves estables.");
    }
    else
    {
        var privPath = config["Jwt:PrivateKeyPath"] ?? "/run/secrets/jwt_private";
        var pubPath = config["Jwt:PublicKeyPath"] ?? "/run/secrets/jwt_public";
        if (!TryReadJwtKeyFiles(contentRoot, privPath, pubPath, out privatePem, out publicPem))
            throw new FileNotFoundException(
                $"JWT keys no encontradas. Paths: {privPath}, {pubPath}. " +
                "Ejecuta ./scripts/gen-secrets.sh o Jwt:DevGenerate=true.");
    }

    return new JwtSettings(
        PrivateKeyPem: privatePem,
        PublicKeyPem: publicPem,
        Issuer: issuer,
        Audience: audience,
        AccessTtl: TimeSpan.FromMinutes(accessTtlMin),
        RefreshTtl: TimeSpan.FromDays(refreshTtlDays));
}

internal sealed record HealthResponse(string Status, string Service, string Version);

public partial class Program;

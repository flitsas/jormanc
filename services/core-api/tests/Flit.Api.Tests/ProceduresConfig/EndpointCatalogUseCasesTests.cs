using System.Text.Json;
using FluentAssertions;
using Flit.Modules.ProceduresConfig.Adapters;
using Flit.Modules.ProceduresConfig.Application;
using Flit.Modules.ProceduresConfig.Domain;
using Xunit;

namespace Flit.Api.Tests.ProceduresConfig;

/// <summary>HU #9439 — CRUD catálogo y rate limit en invocación.</summary>
public sealed class EndpointCatalogUseCasesTests
{
    private static readonly Guid TenantId = InMemoryProcedureRulesRepository.DemoTenantId;
    private static readonly Guid ActorId = Guid.Parse("00000000-0000-7000-8000-000000000001");

    [Fact]
    public async Task AC1_Create_endpoint_with_vault_reference_persists()
    {
        var repo = new InMemoryEndpointCatalogRepository();
        using var authDoc = JsonDocument.Parse("""{"secret_ref":"vault://test-key"}""");
        var auth = authDoc.RootElement;

        var result = await CreateEndpointCatalogEntry.HandleAsync(
            new CreateEndpointCatalogEntry.Command(
                TenantId,
                "TEST_HOOK",
                "Test Hook",
                "https://httpbin.org/post",
                "POST",
                "api_key",
                auth,
                3000,
                true,
                ActorId),
            repo);

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthConfig.TryGetProperty("secret_ref", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AC1_Create_rejects_plaintext_secret()
    {
        var repo = new InMemoryEndpointCatalogRepository();
        using var authDoc = JsonDocument.Parse("""{"password":"super-secret-value-here"}""");
        var auth = authDoc.RootElement;

        var result = await CreateEndpointCatalogEntry.HandleAsync(
            new CreateEndpointCatalogEntry.Command(
                TenantId,
                "BAD_EP",
                "Bad",
                "https://example.com",
                "GET",
                "basic",
                auth,
                5000,
                true,
                ActorId),
            repo);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(EndpointCatalogErrorKind.Validation);
    }

    [Fact]
    public async Task AC2_Invoke_writes_call_log()
    {
        var catalog = new InMemoryEndpointCatalogRepository();
        var callLog = new InMemoryEndpointCallLogRepository();
        var limiter = new EndpointInvocationRateLimiter(maxPerMinute: 100);
        using var handler = new HttpClientHandler();
        var factory = new TestHttpClientFactory(new HttpClient(handler));

        var result = await InvokeCatalogEndpoint.HandleAsync(
            new InvokeCatalogEndpoint.Command(
                TenantId,
                InMemoryEndpointCatalogRepository.DemoEndpointCode),
            catalog,
            callLog,
            limiter,
            factory);

        result.IsSuccess.Should().BeTrue();
        callLog.Entries.Should().ContainSingle(e =>
            e.EndpointCode == InMemoryEndpointCatalogRepository.DemoEndpointCode &&
            e.TenantId == TenantId);
    }

    [Fact]
    public async Task AC1_Rate_limit_returns_429_kind()
    {
        var catalog = new InMemoryEndpointCatalogRepository();
        var callLog = new InMemoryEndpointCallLogRepository();
        var limiter = new EndpointInvocationRateLimiter(maxPerMinute: 1);
        var factory = new TestHttpClientFactory(new HttpClient());

        var first = await InvokeCatalogEndpoint.HandleAsync(
            new InvokeCatalogEndpoint.Command(
                TenantId,
                InMemoryEndpointCatalogRepository.DemoEndpointCode),
            catalog,
            callLog,
            limiter,
            factory);
        first.IsSuccess.Should().BeTrue();

        var second = await InvokeCatalogEndpoint.HandleAsync(
            new InvokeCatalogEndpoint.Command(
                TenantId,
                InMemoryEndpointCatalogRepository.DemoEndpointCode),
            catalog,
            callLog,
            limiter,
            factory);

        second.IsSuccess.Should().BeFalse();
        second.Error.Kind.Should().Be(EndpointCatalogErrorKind.RateLimited);
    }

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}

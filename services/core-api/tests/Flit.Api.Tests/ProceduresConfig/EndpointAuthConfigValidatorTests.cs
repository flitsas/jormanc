using System.Text.Json;
using FluentAssertions;
using Flit.Modules.ProceduresConfig.Domain;
using Xunit;

namespace Flit.Api.Tests.ProceduresConfig;

/// <summary>HU #9439 AC1 — auth por referencia vault://, sin plaintext.</summary>
public sealed class EndpointAuthConfigValidatorTests
{
    [Fact]
    public void Accepts_vault_secret_ref_for_api_key()
    {
        using var config = ParseJson("""{"secret_ref":"vault://rues-key"}""");
        EndpointAuthConfigValidator.TryValidate("api_key", config.RootElement, out var error).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Rejects_plaintext_api_key_property()
    {
        using var config = ParseJson("""{"api_key":"sk-abc123def456"}""");
        EndpointAuthConfigValidator.TryValidate("api_key", config.RootElement, out var error).Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Rejects_none_auth_with_non_empty_config()
    {
        using var config = ParseJson("""{"secret_ref":"vault://x"}""");
        EndpointAuthConfigValidator.TryValidate("none", config.RootElement, out _).Should().BeFalse();
    }

    private static JsonDocument ParseJson(string json) => JsonDocument.Parse(json);
}

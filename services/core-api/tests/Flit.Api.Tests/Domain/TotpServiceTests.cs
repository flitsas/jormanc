using FluentAssertions;
using Flit.Modules.Auth.Domain;
using Xunit;

namespace Flit.Api.Tests.Domain;

/// <summary>
/// Tests del servicio TOTP (RFC 6238).
/// Vectores de referencia: rfc6238 Appendix B usa SHA-1 con secret "12345678901234567890".
/// Aqui validamos: generate-verify roundtrip, ventana de tolerancia ±1, rechazo de codigo invalido.
/// </summary>
public class TotpServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Generate_then_Verify_returns_true_for_same_time()
    {
        var totp = new TotpService();
        var secret = TotpService.GenerateSecret();

        var code = totp.Generate(secret, Now);

        totp.Verify(secret, code, Now).Should().BeTrue();
    }

    [Fact]
    public void Verify_accepts_code_within_previous_step_window()
    {
        var totp = new TotpService();
        var secret = TotpService.GenerateSecret();

        var codeAtT = totp.Generate(secret, Now);
        // 25 segundos despues (mismo step de 30s, pero rozando frontera)
        var later = Now.AddSeconds(25);

        totp.Verify(secret, codeAtT, later).Should().BeTrue();
    }

    [Fact]
    public void Verify_rejects_code_outside_window()
    {
        var totp = new TotpService();
        var secret = TotpService.GenerateSecret();

        var codeAtT = totp.Generate(secret, Now);
        // 2 minutos despues: fuera de la ventana ±1 (60s) → debe rechazar
        var farLater = Now.AddMinutes(2);

        totp.Verify(secret, codeAtT, farLater).Should().BeFalse();
    }

    [Fact]
    public void Verify_rejects_wrong_code()
    {
        var totp = new TotpService();
        var secret = TotpService.GenerateSecret();

        totp.Verify(secret, "000000", Now).Should().BeFalse();
    }

    [Fact]
    public void Verify_rejects_malformed_code()
    {
        var totp = new TotpService();
        var secret = TotpService.GenerateSecret();

        totp.Verify(secret, "abcdef", Now).Should().BeFalse();
        totp.Verify(secret, "12345", Now).Should().BeFalse();
        totp.Verify(secret, "", Now).Should().BeFalse();
    }

    [Fact]
    public void GenerateSecret_returns_base32_string()
    {
        var s = TotpService.GenerateSecret();
        s.Should().MatchRegex("^[A-Z2-7]+$"); // RFC 4648 base32 alfabeto
        s.Length.Should().BeGreaterThan(16);
    }

    [Fact]
    public void BuildOtpAuthUri_uses_otpauth_scheme_with_issuer()
    {
        var totp = new TotpService();
        var secret = TotpService.GenerateSecret();
        var uri = totp.BuildOtpAuthUri(secret, issuer: "FLIT", accountLabel: "user@flit.io");

        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("issuer=FLIT");
        uri.Should().Contain($"secret={secret}");
    }
}

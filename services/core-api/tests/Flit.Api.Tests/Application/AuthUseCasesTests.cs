using System.Text;
using System.Text.Json;
using FluentAssertions;
using Flit.Modules.Auth.Adapters;
using Flit.Modules.Auth.Application;
using Flit.Modules.Auth.Domain;
using Flit.Modules.Users.Adapters;
using Flit.Modules.Users.Domain;
using Flit.Modules.Users.Ports;
using Flit.SharedKernel;
using Xunit;

namespace Flit.Api.Tests.Application;

/// <summary>
/// Tests del modulo Auth (Fase 7 — Hybrid Cognito + MFA TOTP).
/// Cubren flujos criticos:
///   - Login sin MFA → TokensIssued
///   - Login con MFA habilitado → MfaRequired (sessionId)
///   - Login con user no encontrado en BD (sub Cognito huerfano) → CognitoOrphan
///   - ValidateMfa codigo correcto → tokens
///   - ValidateMfa codigo incorrecto → IncrementAttempts + InvalidCode
///   - ValidateMfa demasiados intentos → TooManyAttempts
///   - EnableMfa happy path → secret + URI
///   - EnableMfa ya habilitado → AlreadyEnabled
/// </summary>
public class AuthUseCasesTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = AuthUseCasesTests.Now;
    }

    /// <summary>
    /// Stub de ICognitoDirectory que emite un IdToken con un sub controlado
    /// (formato JWT valido para que Login.ExtractSubFromJwt lo decodifique).
    /// </summary>
    private sealed class FakeJwtCognitoDirectory : ICognitoDirectory
    {
        private readonly string _subToEmit;
        public bool GlobalSignOutCalled { get; private set; }
        public bool ShouldThrowOnLogin { get; set; }

        public FakeJwtCognitoDirectory(string sub) => _subToEmit = sub;

        private CognitoTokens BuildTokens() => new(
            AccessToken: "access-" + Guid.CreateVersion7(),
            IdToken: BuildIdToken(_subToEmit),
            RefreshToken: "refresh-" + Guid.CreateVersion7(),
            ExpiresIn: 3600);

        public Task<CognitoTokens> AdminInitiateAuthAsync(
            string username, string password, CancellationToken ct = default)
        {
            if (ShouldThrowOnLogin)
                throw new InvalidOperationException("NotAuthorizedException: bad creds");
            return Task.FromResult(BuildTokens());
        }

        public Task<CognitoTokens> AdminRefreshAuthAsync(
            string refreshToken, CancellationToken ct = default) => Task.FromResult(BuildTokens());

        public Task AdminUserGlobalSignOutAsync(string username, CancellationToken ct = default)
        {
            GlobalSignOutCalled = true;
            return Task.CompletedTask;
        }

        public Task<CognitoUserResult> AdminCreateUserAsync(
            string email, string tempPassword, Guid appUserId, CancellationToken ct = default)
            => Task.FromResult(new CognitoUserResult(_subToEmit, email));

        public Task AdminSetUserPasswordAsync(
            string username, string newPassword, bool permanent, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task AdminDisableUserAsync(string username, CancellationToken ct = default) => Task.CompletedTask;
        public Task AdminEnableUserAsync(string username, CancellationToken ct = default) => Task.CompletedTask;
        public Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default) => Task.CompletedTask;

        private static string BuildIdToken(string sub)
        {
            var header = ToBase64Url("{\"alg\":\"RS256\",\"typ\":\"JWT\"}");
            var payload = ToBase64Url(JsonSerializer.Serialize(new { sub }));
            return $"{header}.{payload}.signature-placeholder";
        }

        private static string ToBase64Url(string raw) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
                   .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static (User user, IUsersRepository repo) NewUser(string email, string sub, bool mfa = false, Aes256GcmSecretCipher? cipher = null, string? secretB32 = null)
    {
        var user = User.Create(email, "Test User", "CC", "123", "300", null, Now);
        user.LinkCognitoSub(sub, Now);
        if (mfa)
        {
            if (cipher is null || secretB32 is null)
                throw new InvalidOperationException("Cipher+secret requeridos para MFA");
            var encrypted = cipher.Encrypt(secretB32);
            user.EnableMfa(encrypted, Now);
        }
        var repo = new InMemoryUsersRepository();
        repo.AddAsync(user).GetAwaiter().GetResult();
        return (user, repo);
    }

    // ─── Login ────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_no_mfa_returns_TokensIssued()
    {
        const string sub = "cognito-sub-123";
        var (_, repo) = NewUser("u1@flit.io", sub);
        var cognito = new FakeJwtCognitoDirectory(sub);
        var clock = new FixedClock();
        var mfaStore = new InMemoryMfaSessionStore();

        var result = await Login.HandleAsync(new Login.Command("u1@flit.io", "pwd", null, null), repo, cognito, mfaStore, clock, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<Login.Response.TokensIssued>();
    }

    [Fact]
    public async Task Login_with_mfa_enabled_returns_MfaRequired_with_sessionId()
    {
        const string sub = "cognito-sub-mfa";
        var cipher = new Aes256GcmSecretCipher(NewKey(), "v1");
        var secret = TotpService.GenerateSecret();
        var (_, repo) = NewUser("u2@flit.io", sub, mfa: true, cipher: cipher, secretB32: secret);
        var cognito = new FakeJwtCognitoDirectory(sub);
        var clock = new FixedClock();
        var mfaStore = new InMemoryMfaSessionStore();

        var result = await Login.HandleAsync(new Login.Command("u2@flit.io", "pwd", null, null), repo, cognito, mfaStore, clock, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var mfaResp = result.Value.Should().BeOfType<Login.Response.MfaRequired>().Subject;
        mfaResp.SessionId.Should().NotBeEmpty();

        // La sesion debe estar en el store con los tokens originales.
        var pending = await mfaStore.GetAsync(mfaResp.SessionId, TestContext.Current.CancellationToken);
        pending.Should().NotBeNull();
        pending!.CognitoSub.Should().Be(sub);
    }

    [Fact]
    public async Task Login_with_user_not_in_db_returns_CognitoOrphan_and_signs_out()
    {
        var cognito = new FakeJwtCognitoDirectory("unknown-sub");
        var repo = new InMemoryUsersRepository();
        var result = await Login.HandleAsync(new Login.Command("ghost@flit.io", "pwd", null, null), repo, cognito, new InMemoryMfaSessionStore(), new FixedClock(), TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<Login.LoginError.CognitoOrphan>();
        cognito.GlobalSignOutCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Login_with_invalid_credentials_returns_InvalidCredentials()
    {
        var cognito = new FakeJwtCognitoDirectory("x") { ShouldThrowOnLogin = true };
        var result = await Login.HandleAsync(new Login.Command("x@flit.io", "wrong", null, null), new InMemoryUsersRepository(), cognito, new InMemoryMfaSessionStore(), new FixedClock(), TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<Login.LoginError.InvalidCredentials>();
    }

    // ─── ValidateMfa ──────────────────────────────────────────────────

    [Fact]
    public async Task ValidateMfa_with_correct_code_returns_tokens_and_clears_session()
    {
        const string sub = "sub-vm-1";
        var cipher = new Aes256GcmSecretCipher(NewKey(), "v1");
        var totp = new TotpService();
        var secret = TotpService.GenerateSecret();
        var (user, repo) = NewUser("vm1@flit.io", sub, mfa: true, cipher: cipher, secretB32: secret);
        var cognito = new FakeJwtCognitoDirectory(sub);
        var clock = new FixedClock();
        var mfaStore = new InMemoryMfaSessionStore();

        // Step 1: login para crear la sesion pendiente.
        var login = await Login.HandleAsync(new Login.Command("vm1@flit.io", "pwd", null, null), repo, cognito, mfaStore, clock, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        var sessionId = ((Login.Response.MfaRequired)login.Value).SessionId;

        // Step 2: validar con codigo correcto.
        var code = totp.Generate(secret, Now);
        var result = await ValidateMfa.HandleAsync(new ValidateMfa.Command(sessionId, code), mfaStore, repo, cognito, cipher, totp, clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Tokens.AccessToken.Should().NotBeNullOrEmpty();

        // Sesion debe estar borrada.
        (await mfaStore.GetAsync(sessionId, TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task ValidateMfa_with_wrong_code_returns_InvalidCode_and_increments_attempts()
    {
        const string sub = "sub-vm-2";
        var cipher = new Aes256GcmSecretCipher(NewKey(), "v1");
        var secret = TotpService.GenerateSecret();
        var (_, repo) = NewUser("vm2@flit.io", sub, mfa: true, cipher: cipher, secretB32: secret);
        var cognito = new FakeJwtCognitoDirectory(sub);
        var clock = new FixedClock();
        var mfaStore = new InMemoryMfaSessionStore();
        var totp = new TotpService();

        var login = await Login.HandleAsync(new Login.Command("vm2@flit.io", "pwd", null, null), repo, cognito, mfaStore, clock, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        var sessionId = ((Login.Response.MfaRequired)login.Value).SessionId;

        var result = await ValidateMfa.HandleAsync(new ValidateMfa.Command(sessionId, "000000"), mfaStore, repo, cognito, cipher, totp, clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ValidateMfa.ValidateMfaError.InvalidCode>();

        // La sesion debe seguir viva con attempts=1.
        var pending = await mfaStore.GetAsync(sessionId, TestContext.Current.CancellationToken);
        pending!.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task ValidateMfa_with_too_many_attempts_returns_TooManyAttempts()
    {
        const string sub = "sub-vm-3";
        var cipher = new Aes256GcmSecretCipher(NewKey(), "v1");
        var secret = TotpService.GenerateSecret();
        var (_, repo) = NewUser("vm3@flit.io", sub, mfa: true, cipher: cipher, secretB32: secret);
        var cognito = new FakeJwtCognitoDirectory(sub);
        var clock = new FixedClock();
        var mfaStore = new InMemoryMfaSessionStore();
        var totp = new TotpService();

        var login = await Login.HandleAsync(new Login.Command("vm3@flit.io", "pwd", null, null), repo, cognito, mfaStore, clock, TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
        var sessionId = ((Login.Response.MfaRequired)login.Value).SessionId;

        // 3 intentos fallidos llevan attempts a 3; el 4to choque dispara TooManyAttempts.
        for (int i = 0; i < 3; i++)
        {
            await ValidateMfa.HandleAsync(new ValidateMfa.Command(sessionId, "000000"), mfaStore, repo, cognito, cipher, totp, clock, TestContext.Current.CancellationToken);
        }

        var final = await ValidateMfa.HandleAsync(new ValidateMfa.Command(sessionId, "000000"), mfaStore, repo, cognito, cipher, totp, clock, TestContext.Current.CancellationToken);

        final.IsSuccess.Should().BeFalse();
        final.Error.Should().BeOfType<ValidateMfa.ValidateMfaError.TooManyAttempts>();
        cognito.GlobalSignOutCalled.Should().BeTrue();
        (await mfaStore.GetAsync(sessionId, TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task ValidateMfa_with_missing_session_returns_SessionExpired()
    {
        var result = await ValidateMfa.HandleAsync(new ValidateMfa.Command(Guid.CreateVersion7(), "123456"), new InMemoryMfaSessionStore(), new InMemoryUsersRepository(), new FakeJwtCognitoDirectory("x"), new Aes256GcmSecretCipher(NewKey(), "v1"), new TotpService(), new FixedClock(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ValidateMfa.ValidateMfaError.SessionExpired>();
    }

    // ─── EnableMfa / DisableMfa ───────────────────────────────────────

    [Fact]
    public async Task EnableMfa_on_active_user_returns_secret_and_uri()
    {
        var (user, repo) = NewUser("em@flit.io", "sub-em");
        var cipher = new Aes256GcmSecretCipher(NewKey(), "v1");
        var totp = new TotpService();
        var clock = new FixedClock();

        var result = await EnableMfa.HandleAsync(new EnableMfa.Command(user.Id), repo, cipher, totp, "FLIT", clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.SecretBase32.Should().MatchRegex("^[A-Z2-7]+$");
        result.Value.OtpAuthUri.Should().StartWith("otpauth://totp/");

        var reloaded = await repo.GetByIdAsync(user.Id, TestContext.Current.CancellationToken);
        reloaded!.MfaEnabled.Should().BeTrue();
        reloaded.MfaSecret.Should().NotBeNull();
    }

    [Fact]
    public async Task EnableMfa_already_enabled_returns_AlreadyEnabled()
    {
        var cipher = new Aes256GcmSecretCipher(NewKey(), "v1");
        var (user, repo) = NewUser("em2@flit.io", "sub-em2", mfa: true, cipher: cipher, secretB32: TotpService.GenerateSecret());

        var result = await EnableMfa.HandleAsync(new EnableMfa.Command(user.Id), repo, new Aes256GcmSecretCipher(NewKey(), "v1"), new TotpService(), "FLIT", new FixedClock(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<EnableMfa.EnableMfaError.AlreadyEnabled>();
    }

    [Fact]
    public async Task DisableMfa_unsets_secret()
    {
        var cipher = new Aes256GcmSecretCipher(NewKey(), "v1");
        var (user, repo) = NewUser("dm@flit.io", "sub-dm", mfa: true, cipher: cipher, secretB32: TotpService.GenerateSecret());

        var result = await DisableMfa.HandleAsync(new DisableMfa.Command(user.Id), repo, new FixedClock(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await repo.GetByIdAsync(user.Id, TestContext.Current.CancellationToken);
        reloaded!.MfaEnabled.Should().BeFalse();
        reloaded.MfaSecret.Should().BeNull();
    }

    private static byte[] NewKey()
    {
        var k = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(k);
        return k;
    }
}

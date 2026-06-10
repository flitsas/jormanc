using FluentAssertions;
using Flit.Modules.Users.Adapters;
using Flit.Modules.Users.Application;
using Flit.Modules.Users.Domain;
using Flit.Modules.Users.Ports;
using Flit.SharedKernel;
using Xunit;

namespace Flit.Api.Tests.Application;

/// <summary>
/// Unit tests de los use cases del modulo Users (ADR-0010).
/// Usan adapters InMemory + stubs propios (sin librería de mocks).
/// Cubren happy path + edge cases + compensación Cognito.
/// </summary>
public class UsersUseCasesTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);
    private static IClock Clock => new FixedClock(Now);

    // ─── CreateUser ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_OK_returns_user_with_cognito_sub_linked()
    {
        var (repo, cognito, uow, pwgen) = MakeUserStubs();
        var cmd = new CreateUser.Command(
            Email: "Mateo.Ruiz@FLIT.IO",
            FullName: "Mateo Ruiz",
            DocumentType: "CC",
            DocumentNumber: "1020304050",
            Phone: "3001112233",
            CreatedByUserId: null);

        var result = await CreateUser.HandleAsync(cmd, repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Id.Should().NotBeEmpty();
        response.Email.Should().Be("mateo.ruiz@flit.io"); // normalized lowercase
        response.TempPassword.Should().NotBeNullOrEmpty();
        response.TempPassword.Length.Should().BeGreaterOrEqualTo(8);

        var saved = await repo.GetByIdAsync(response.Id, TestContext.Current.CancellationToken);
        saved.Should().NotBeNull();
        saved!.CognitoSub.Should().StartWith("stub-"); // StubCognitoDirectory
    }

    [Fact]
    public async Task CreateUser_EmailDuplicated_returns_409_equivalent_error()
    {
        var (repo, cognito, uow, pwgen) = MakeUserStubs();
        var first = new CreateUser.Command("dup@flit.io", "A", null, null, null, null);
        await CreateUser.HandleAsync(first, repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        var second = new CreateUser.Command("dup@flit.io", "B", null, null, null, null);
        var result = await CreateUser.HandleAsync(second, repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<CreateUser.CreateUserError.EmailDuplicated>();
        result.Error.Code.Should().Be("EMAIL_DUPLICATED");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateUser_validates_empty_email(string email)
    {
        var (repo, cognito, uow, pwgen) = MakeUserStubs();
        var cmd = new CreateUser.Command(email, "Mateo", null, null, null, null);

        var result = await CreateUser.HandleAsync(cmd, repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<CreateUser.CreateUserError.ValidationFailed>();
    }

    [Fact]
    public async Task CreateUser_CognitoFails_returns_CognitoSyncFail_error()
    {
        // Verifica que el use case captura la excepcion de Cognito y devuelve
        // Result.Failure con CreateUserError.CognitoSyncFail.
        //
        // El rollback REAL de BD se valida en integration tests con EfUnitOfWork
        // + Postgres real. InMemoryUnitOfWork es no-op por diseno — el user
        // queda persistido in-memory aunque el use case devuelva Failure. Esto
        // es OK porque la responsabilidad del rollback es del UoW, no del use case.
        var (repo, _, uow, pwgen) = MakeUserStubs();
        var failingCognito = new FailingCognitoDirectory();
        var cmd = new CreateUser.Command("fail@flit.io", "X", null, null, null, null);

        var result = await CreateUser.HandleAsync(cmd, repo, failingCognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<CreateUser.CreateUserError.CognitoSyncFail>();
        result.Error.Code.Should().Be("COGNITO_SYNC_FAIL");
        result.Error.Message.Should().Contain("COGNITO_NOT_AUTHORIZED");
    }

    // ─── GetUser ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUser_returns_null_when_not_found()
    {
        var (repo, _, _, _) = MakeUserStubs();
        var result = await GetUser.HandleAsync(new GetUser.Query(Guid.CreateVersion7()), repo, TestContext.Current.CancellationToken);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUser_returns_response_when_found()
    {
        var (repo, cognito, uow, pwgen) = MakeUserStubs();
        var created = await CreateUser.HandleAsync(new CreateUser.Command("get@flit.io", "Get Test", null, null, null, null), repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        var found = await GetUser.HandleAsync(new GetUser.Query(created.Value.Id), repo, TestContext.Current.CancellationToken);

        found.Should().NotBeNull();
        found!.Email.Should().Be("get@flit.io");
        found.FullName.Should().Be("Get Test");
        found.Status.Should().Be(UserStatus.ACTIVE);
    }

    [Fact]
    public async Task GetUser_returns_null_when_soft_deleted()
    {
        var (repo, cognito, uow, pwgen) = MakeUserStubs();
        var created = await CreateUser.HandleAsync(new CreateUser.Command("del@flit.io", "X", null, null, null, null), repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);
        await DeleteUser.HandleAsync(new DeleteUser.Command(created.Value.Id, null), repo, cognito, uow, Clock, TestContext.Current.CancellationToken);

        var found = await GetUser.HandleAsync(new GetUser.Query(created.Value.Id), repo, TestContext.Current.CancellationToken);
        found.Should().BeNull();
    }

    // ─── ListUsers ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListUsers_returns_paginated_results()
    {
        var (repo, cognito, uow, pwgen) = MakeUserStubs();
        for (int i = 1; i <= 5; i++)
        {
            await CreateUser.HandleAsync(new CreateUser.Command($"user{i}@flit.io", $"User {i}", null, null, null, null), repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);
        }

        var page1 = await ListUsers.HandleAsync(new ListUsers.Query(1, 2, null), repo, TestContext.Current.CancellationToken);

        page1.Total.Should().Be(5);
        page1.Page.Should().Be(1);
        page1.Limit.Should().Be(2);
        page1.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListUsers_filters_by_search()
    {
        var (repo, cognito, uow, pwgen) = MakeUserStubs();
        await CreateUser.HandleAsync(new CreateUser.Command("mateo@flit.io", "Mateo Ruiz", null, null, null, null), repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);
        await CreateUser.HandleAsync(new CreateUser.Command("ana@flit.io", "Ana Lopez", null, null, null, null), repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        var byName = await ListUsers.HandleAsync(new ListUsers.Query(1, 20, "mateo"), repo, TestContext.Current.CancellationToken);

        byName.Total.Should().Be(1);
        byName.Data[0].FullName.Should().Be("Mateo Ruiz");
    }

    // ─── UpdateUser ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_updates_profile_fields()
    {
        var (repo, cognito, uow, pwgen) = MakeUserStubs();
        var created = await CreateUser.HandleAsync(new CreateUser.Command("upd@flit.io", "Old Name", null, null, null, null), repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        var result = await UpdateUser.HandleAsync(new UpdateUser.Command(created.Value.Id, "New Name", "CC", "999", "300"), repo, Clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("New Name");
        result.Value.DocumentNumber.Should().Be("999");
    }

    [Fact]
    public async Task UpdateUser_NotFound_returns_error()
    {
        var (repo, _, _, _) = MakeUserStubs();
        var result = await UpdateUser.HandleAsync(new UpdateUser.Command(Guid.CreateVersion7(), "X", null, null, null), repo, Clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<UpdateUser.UpdateUserError.NotFound>();
    }

    // ─── ChangeUserStatus ─────────────────────────────────────────────

    [Fact]
    public async Task ChangeUserStatus_to_BLOCKED_calls_Cognito_disable_and_signout()
    {
        var (repo, _, uow, pwgen) = MakeUserStubs();
        var trackingCognito = new TrackingCognitoDirectory();
        var created = await CreateUser.HandleAsync(new CreateUser.Command("block@flit.io", "X", null, null, null, null), repo, trackingCognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        var result = await ChangeUserStatus.HandleAsync(new ChangeUserStatus.Command(created.Value.Id, UserStatus.BLOCKED, null), repo, trackingCognito, uow, Clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(UserStatus.BLOCKED);
        trackingCognito.DisableCalls.Should().Contain("block@flit.io");
        trackingCognito.GlobalSignOutCalls.Should().Contain("block@flit.io");
    }

    [Fact]
    public async Task ChangeUserStatus_to_DELETED_returns_InvalidTransition()
    {
        var (repo, cognito, uow, pwgen) = MakeUserStubs();
        var created = await CreateUser.HandleAsync(new CreateUser.Command("err@flit.io", "X", null, null, null, null), repo, cognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        var result = await ChangeUserStatus.HandleAsync(new ChangeUserStatus.Command(created.Value.Id, UserStatus.DELETED, null), repo, cognito, uow, Clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<ChangeUserStatus.ChangeStatusError.InvalidTransition>();
    }

    // ─── DeleteUser ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_soft_deletes_and_calls_Cognito_disable()
    {
        var (repo, _, uow, pwgen) = MakeUserStubs();
        var trackingCognito = new TrackingCognitoDirectory();
        var created = await CreateUser.HandleAsync(new CreateUser.Command("kill@flit.io", "X", null, null, null, null), repo, trackingCognito, uow, Clock, pwgen, TestContext.Current.CancellationToken);

        var result = await DeleteUser.HandleAsync(new DeleteUser.Command(created.Value.Id, null), repo, trackingCognito, uow, Clock, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        trackingCognito.DisableCalls.Should().Contain("kill@flit.io");
        trackingCognito.GlobalSignOutCalls.Should().Contain("kill@flit.io");

        // El user sigue existiendo en BD (soft delete) pero con DeletedAt seteado
        // — GetByIdAsync lo devuelve sin filtro.
        var stillThere = await repo.GetByIdAsync(created.Value.Id, TestContext.Current.CancellationToken);
        stillThere.Should().NotBeNull();
        stillThere!.DeletedAt.Should().NotBeNull();
        stillThere.Status.Should().Be(UserStatus.DELETED);
    }

    // ─── Helpers ───────────────────────────────────────────────────────

    private static (IUsersRepository repo, ICognitoDirectory cognito, IUnitOfWork uow, IPasswordGenerator pwgen) MakeUserStubs()
    {
        return (
            new InMemoryUsersRepository(),
            new StubCognitoDirectory(),
            new InMemoryUnitOfWork(),
            new TempPasswordGenerator()
        );
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    /// <summary>Cognito que falla en AdminCreateUser para testar rollback.</summary>
    private sealed class FailingCognitoDirectory : ICognitoDirectory
    {
        public Task<CognitoUserResult> AdminCreateUserAsync(string email, string tempPassword, Guid appUserId, CancellationToken ct = default)
            => throw new InvalidOperationException("COGNITO_NOT_AUTHORIZED");
        public Task AdminSetUserPasswordAsync(string username, string newPassword, bool permanent, CancellationToken ct = default) => Task.CompletedTask;
        public Task<CognitoTokens> AdminInitiateAuthAsync(string username, string password, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<CognitoTokens> AdminRefreshAuthAsync(string refreshToken, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AdminDisableUserAsync(string username, CancellationToken ct = default) => Task.CompletedTask;
        public Task AdminEnableUserAsync(string username, CancellationToken ct = default) => Task.CompletedTask;
        public Task AdminUserGlobalSignOutAsync(string username, CancellationToken ct = default) => Task.CompletedTask;
        public Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default) => Task.CompletedTask;
    }

    /// <summary>Cognito que registra las llamadas para verificar side effects.</summary>
    private sealed class TrackingCognitoDirectory : ICognitoDirectory
    {
        public List<string> DisableCalls { get; } = [];
        public List<string> EnableCalls { get; } = [];
        public List<string> GlobalSignOutCalls { get; } = [];

        public Task<CognitoUserResult> AdminCreateUserAsync(string email, string tempPassword, Guid appUserId, CancellationToken ct = default)
            => Task.FromResult(new CognitoUserResult($"stub-{Guid.CreateVersion7()}", email));
        public Task AdminSetUserPasswordAsync(string u, string p, bool perm, CancellationToken ct = default) => Task.CompletedTask;
        public Task<CognitoTokens> AdminInitiateAuthAsync(string u, string p, CancellationToken ct = default)
            => Task.FromResult(new CognitoTokens("a", "i", "r", 3600));
        public Task<CognitoTokens> AdminRefreshAuthAsync(string r, CancellationToken ct = default)
            => Task.FromResult(new CognitoTokens("a", "i", r, 3600));
        public Task AdminDisableUserAsync(string u, CancellationToken ct = default) { DisableCalls.Add(u); return Task.CompletedTask; }
        public Task AdminEnableUserAsync(string u, CancellationToken ct = default) { EnableCalls.Add(u); return Task.CompletedTask; }
        public Task AdminUserGlobalSignOutAsync(string u, CancellationToken ct = default) { GlobalSignOutCalls.Add(u); return Task.CompletedTask; }
        public Task RevokeTokenAsync(string r, CancellationToken ct = default) => Task.CompletedTask;
    }
}

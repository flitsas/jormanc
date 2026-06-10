using FluentAssertions;
using Flit.Modules.Users.Domain;
using Xunit;

namespace Flit.Api.Tests.Domain;

/// <summary>
/// Unit tests del aggregate User (ADR-0010).
/// Verifican invariantes de dominio: validacion en Create, transiciones de Status,
/// asociacion de cognito_sub, soft delete, MFA enable/disable.
/// </summary>
public class UserDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_email_to_lowercase_and_trims()
    {
        var user = User.Create("  TEST@FLIT.IO  ", "Mateo Ruiz",
            documentType: "CC", documentNumber: "123456",
            phone: "3000000000", createdByUserId: null, now: Now);

        user.Email.Should().Be("test@flit.io");
        user.FullName.Should().Be("Mateo Ruiz");
        user.Status.Should().Be(UserStatus.ACTIVE);
        user.MfaEnabled.Should().BeFalse();
        user.CognitoSub.Should().BeNull();
        user.DeletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_throws_on_empty_email(string? email)
    {
        Action act = () => User.Create(email!, "x", null, null, null, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_throws_on_empty_full_name(string? name)
    {
        Action act = () => User.Create("a@b.io", name!, null, null, null, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("fullName");
    }

    [Fact]
    public void Create_generates_UUIDv7_id()
    {
        var u1 = User.Create("a@b.io", "A", null, null, null, null, Now);
        var u2 = User.Create("c@d.io", "C", null, null, null, null, Now);
        u1.Id.Should().NotBe(u2.Id);
        u1.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void LinkCognitoSub_sets_sub_and_updates_timestamp()
    {
        var u = User.Create("a@b.io", "A", null, null, null, null, Now);
        var later = Now.AddMinutes(5);

        u.LinkCognitoSub("cognito-sub-123", later);

        u.CognitoSub.Should().Be("cognito-sub-123");
        u.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void LinkCognitoSub_throws_when_already_linked()
    {
        var u = User.Create("a@b.io", "A", null, null, null, null, Now);
        u.LinkCognitoSub("sub1", Now);

        Action act = () => u.LinkCognitoSub("sub2", Now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ChangeStatus_to_BLOCKED_updates_status_and_timestamp()
    {
        var u = User.Create("a@b.io", "A", null, null, null, null, Now);
        var later = Now.AddHours(1);

        u.ChangeStatus(UserStatus.BLOCKED, later);

        u.Status.Should().Be(UserStatus.BLOCKED);
        u.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void ChangeStatus_is_idempotent_when_same_status()
    {
        var u = User.Create("a@b.io", "A", null, null, null, null, Now);
        var originalUpdate = u.UpdatedAt;
        var later = Now.AddHours(1);

        u.ChangeStatus(UserStatus.ACTIVE, later); // ya ACTIVE

        u.UpdatedAt.Should().Be(originalUpdate); // no se toca
    }

    [Fact]
    public void ChangeStatus_throws_when_already_deleted()
    {
        var u = User.Create("a@b.io", "A", null, null, null, null, Now);
        u.Delete(Now);

        Action act = () => u.ChangeStatus(UserStatus.ACTIVE, Now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Delete_sets_status_DELETED_and_deletedAt()
    {
        var u = User.Create("a@b.io", "A", null, null, null, null, Now);
        var later = Now.AddDays(1);

        u.Delete(later);

        u.Status.Should().Be(UserStatus.DELETED);
        u.DeletedAt.Should().Be(later);
        u.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void EnableMfa_only_works_when_ACTIVE()
    {
        var u = User.Create("a@b.io", "A", null, null, null, null, Now);
        u.ChangeStatus(UserStatus.BLOCKED, Now);
        var secret = MfaSecret.Create(new byte[32], new byte[12], "v1");

        Action act = () => u.EnableMfa(secret, Now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnableMfa_sets_secret_and_timestamp_when_ACTIVE()
    {
        var u = User.Create("a@b.io", "A", null, null, null, null, Now);
        var secret = MfaSecret.Create(new byte[32], new byte[12], "v1");
        var later = Now.AddMinutes(1);

        u.EnableMfa(secret, later);

        u.MfaEnabled.Should().BeTrue();
        u.MfaSecret.Should().NotBeNull();
        u.MfaEnabledAt.Should().Be(later);
    }

    [Fact]
    public void DisableMfa_clears_secret()
    {
        var u = User.Create("a@b.io", "A", null, null, null, null, Now);
        u.EnableMfa(MfaSecret.Create(new byte[32], new byte[12], "v1"), Now);
        u.DisableMfa(Now.AddHours(1));

        u.MfaEnabled.Should().BeFalse();
        u.MfaSecret.Should().BeNull();
        u.MfaEnabledAt.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_updates_fields()
    {
        var u = User.Create("a@b.io", "Old", null, null, null, null, Now);
        var later = Now.AddDays(7);

        u.UpdateProfile("New Name", "CC", "999", "300", later);

        u.FullName.Should().Be("New Name");
        u.DocumentType.Should().Be("CC");
        u.DocumentNumber.Should().Be("999");
        u.Phone.Should().Be("300");
        u.UpdatedAt.Should().Be(later);
    }
}

/// <summary>Tests del Value Object MfaSecret.</summary>
public class MfaSecretTests
{
    [Fact]
    public void Create_validates_nonce_length()
    {
        Action act = () => MfaSecret.Create(new byte[32], new byte[10], "v1");
        act.Should().Throw<ArgumentException>().WithMessage("*12 bytes*");
    }

    [Fact]
    public void Create_validates_empty_ciphertext()
    {
        Action act = () => MfaSecret.Create([], new byte[12], "v1");
        act.Should().Throw<ArgumentException>().WithMessage("*Ciphertext*");
    }

    [Fact]
    public void Create_validates_keyId()
    {
        Action act = () => MfaSecret.Create(new byte[32], new byte[12], "");
        act.Should().Throw<ArgumentException>().WithMessage("*KeyId*");
    }
}

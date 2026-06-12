using FluentAssertions;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.Queries;
using Flit.Modules.Identity.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Flit.Identity.Tests.Rbac;

public class ListUsersQueryHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ListUsersQueryHandler _sut;

    public ListUsersQueryHandlerTests() => _sut = new ListUsersQueryHandler(_userRepo);

    [Fact]
    public async Task ListUsers_DevuelveUsuariosPaginadosDelTenant()
    {
        var tenantId = Guid.NewGuid();
        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Slug = "admin",
            Name = "Administrador",
            IsSystem = true
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "operador@acme.com",
            FullName = "Operador Tenant",
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow,
            UserRoles =
            [
                new UserRole { RoleId = role.Id, Role = role, TenantId = tenantId, UserId = Guid.NewGuid() }
            ]
        };

        IReadOnlyList<User> users = new List<User> { user };
        _userRepo.ListByTenantPaginatedAsync(tenantId, 1, 20, null, Arg.Any<CancellationToken>())
            .Returns((users, 1));

        var result = await _sut.HandleAsync(new ListUsersQuery(tenantId));

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Email.Should().Be("operador@acme.com");
        result.Items[0].Roles.Should().ContainSingle(r => r.Slug == "admin");
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task ListUsers_NormalizaPageSizeInvalido()
    {
        var tenantId = Guid.NewGuid();
        IReadOnlyList<User> empty = Array.Empty<User>();
        _userRepo.ListByTenantPaginatedAsync(tenantId, 1, 100, null, Arg.Any<CancellationToken>())
            .Returns((empty, 0));

        var result = await _sut.HandleAsync(new ListUsersQuery(tenantId, Page: 0, PageSize: 500));

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(100);
        await _userRepo.Received(1).ListByTenantPaginatedAsync(
            tenantId, 1, 100, null, Arg.Any<CancellationToken>());
    }
}

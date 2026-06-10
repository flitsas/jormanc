using FluentAssertions;
using NetArchTest.Rules;
using Flit.Modules.Identity.Domain;
using Flit.SharedKernel;
using Xunit;

namespace Flit.ArchTests;

/// <summary>
/// Reglas arquitectonicas inviolables (ADR-0002 §8.1).
/// Validadas sobre el modulo Identity (el unico modulo de dominio que
/// sobrevive el tear-down FLIT 2.0). Los modulos Users, RBAC, Procedures
/// agregaran sus propios tests en Fases 6-7.
/// </summary>
public class ArchitectureRulesTests
{
    private static readonly System.Reflection.Assembly IdentityAssembly = typeof(Usuario).Assembly;
    private static readonly System.Reflection.Assembly SharedKernelAssembly = typeof(Result<,>).Assembly;

    [Fact]
    public void Regla1_Domain_NoDependeDeAdapters()
    {
        var result = Types
            .InAssembly(IdentityAssembly)
            .That()
            .ResideInNamespace("Flit.Modules.Identity.Domain")
            .ShouldNot()
            .HaveDependencyOn("Flit.Modules.Identity.Adapters")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain no debe depender de Adapters (regla arquitectonica 1)");
    }

    [Fact]
    public void Regla2_Domain_NoDependeDeAspNetCore()
    {
        var result = Types
            .InAssembly(IdentityAssembly)
            .That()
            .ResideInNamespace("Flit.Modules.Identity.Domain")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.Hosting",
                "Microsoft.Extensions.DependencyInjection")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain no debe depender de AspNetCore (regla arquitectonica 2)");
    }

    [Fact]
    public void Regla3_Domain_NoDependeDeEfCore()
    {
        var result = Types
            .InAssembly(IdentityAssembly)
            .That()
            .ResideInNamespace("Flit.Modules.Identity.Domain")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain no debe depender de EF Core (regla arquitectonica 3)");
    }

    [Fact]
    public void Regla4_SharedKernel_SinDependenciasExternas()
    {
        var result = Types
            .InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "SharedKernel no debe depender de frameworks externos (regla arquitectonica 4)");
    }
}

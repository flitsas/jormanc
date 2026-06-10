using FluentAssertions;
using NetArchTest.Rules;
using Flit.SharedKernel;
using Xunit;

namespace Flit.ArchTests;

/// <summary>
/// Reglas arquitectonicas inviolables (ADR-0002 §8.1).
/// Esqueleto base FLIT 2.0: tras el reset solo sobrevive SharedKernel, sobre el
/// que se valida que no dependa de frameworks externos. Los modulos de dominio
/// (Users, RBAC, etc.) agregaran sus propias reglas Domain→Adapters al crearse.
/// </summary>
public class ArchitectureRulesTests
{
    private static readonly System.Reflection.Assembly SharedKernelAssembly = typeof(Result<,>).Assembly;

    [Fact]
    public void Regla_SharedKernel_SinDependenciasExternas()
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
            because: "SharedKernel no debe depender de frameworks externos (regla arquitectonica)");
    }
}

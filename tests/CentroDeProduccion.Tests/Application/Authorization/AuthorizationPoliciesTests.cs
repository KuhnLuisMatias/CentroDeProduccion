using CentroDeProduccion.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Authorization;

/// <summary>
/// Verifies the "Reportes y dashboard" authorization policies map to the correct roles. Each
/// policy must resolve to a single role requirement carrying the expected allowed-role set.
/// </summary>
public class AuthorizationPoliciesTests
{
    private static AuthorizationOptions CrearOptions()
    {
        var options = new AuthorizationOptions();
        options.AddReportsAuthorizationPolicies();
        return options;
    }

    private static IEnumerable<string> Roles(AuthorizationOptions options, string policy)
    {
        // .NET 10's RequireRole adds a RolesAuthorizationRequirement exposing AllowedRoles.
        var requirement = options.GetPolicy(policy)!.Requirements
            .Single(r => r.GetType().GetProperty("AllowedRoles") is not null);
        return (IEnumerable<string>)requirement.GetType().GetProperty("AllowedRoles")!.GetValue(requirement)!;
    }

    [Fact]
    public void AddReportsAuthorizationPolicies_RegistraLasDiezPoliticas()
    {
        var options = CrearOptions();

        options.GetPolicy(AuthorizationPolicies.CanViewDashboard).ShouldNotBeNull();
        options.GetPolicy(AuthorizationPolicies.CanViewProduccion).ShouldNotBeNull();
        options.GetPolicy(AuthorizationPolicies.CanViewStock).ShouldNotBeNull();
        options.GetPolicy(AuthorizationPolicies.CanViewCompras).ShouldNotBeNull();
        options.GetPolicy(AuthorizationPolicies.CanViewCtaCteProveedor).ShouldNotBeNull();
        options.GetPolicy(AuthorizationPolicies.CanViewVentas).ShouldNotBeNull();
        options.GetPolicy(AuthorizationPolicies.CanViewCtaCteBar).ShouldNotBeNull();
        options.GetPolicy(AuthorizationPolicies.CanViewCostos).ShouldNotBeNull();
        options.GetPolicy(AuthorizationPolicies.CanViewRentabilidad).ShouldNotBeNull();
    }

    [Fact]
    public void PoliticasDeAdministrador_SoloPermitenAdministrador()
    {
        var options = CrearOptions();

        Roles(options, AuthorizationPolicies.CanViewDashboard)
            .ShouldBe(new[] { AuthorizationPolicies.Administrador });
        Roles(options, AuthorizationPolicies.CanViewCostos)
            .ShouldBe(new[] { AuthorizationPolicies.Administrador });
        Roles(options, AuthorizationPolicies.CanViewRentabilidad)
            .ShouldBe(new[] { AuthorizationPolicies.Administrador });
    }

    [Fact]
    public void PoliticasDeProduccion_PermitenAdministradorYEncargadoProduccion()
    {
        var options = CrearOptions();
        var esperado = new[] { AuthorizationPolicies.Administrador, AuthorizationPolicies.EncargadoProduccion };

        Roles(options, AuthorizationPolicies.CanViewProduccion).ShouldBe(esperado, ignoreOrder: true);
        Roles(options, AuthorizationPolicies.CanViewStock).ShouldBe(esperado, ignoreOrder: true);
    }

    [Fact]
    public void PoliticasDeCompras_PermitenAdministradorYEncargadoCompras()
    {
        var options = CrearOptions();
        var esperado = new[] { AuthorizationPolicies.Administrador, AuthorizationPolicies.EncargadoCompras };

        Roles(options, AuthorizationPolicies.CanViewCompras).ShouldBe(esperado, ignoreOrder: true);
        Roles(options, AuthorizationPolicies.CanViewCtaCteProveedor).ShouldBe(esperado, ignoreOrder: true);
    }

    [Fact]
    public void PoliticasDeVentas_PermitenAdministradorYEncargadoVentas()
    {
        var options = CrearOptions();
        var esperado = new[] { AuthorizationPolicies.Administrador, AuthorizationPolicies.EncargadoVentas };

        Roles(options, AuthorizationPolicies.CanViewVentas).ShouldBe(esperado, ignoreOrder: true);
        Roles(options, AuthorizationPolicies.CanViewCtaCteBar).ShouldBe(esperado, ignoreOrder: true);
    }
}

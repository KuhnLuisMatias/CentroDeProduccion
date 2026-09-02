using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CentroDeProduccion.Application.Authorization;

/// <summary>
/// Centralizes the authorization policies for the "Reportes y dashboard" module. Policies map
/// report areas to the roles allowed to view them; endpoints attach a policy via
/// <c>[Authorize(Policy = AuthorizationPolicies.CanView...)]</c>.
/// </summary>
public static class AuthorizationPolicies
{
    // Policy names — used as [Authorize(Policy = ...)] on report/dashboard endpoints.
    public const string CanViewDashboard = "CanViewDashboard";
    public const string CanViewProduccion = "CanViewProduccion";
    public const string CanViewStock = "CanViewStock";
    public const string CanViewCompras = "CanViewCompras";
    public const string CanViewCtaCteProveedor = "CanViewCtaCteProveedor";
    public const string CanViewVentas = "CanViewVentas";
    public const string CanViewCtaCteBar = "CanViewCtaCteBar";
    public const string CanViewCostos = "CanViewCostos";
    public const string CanViewRentabilidad = "CanViewRentabilidad";

    // Role names (must match the Rol enum string values used in the JWT role claim).
    public const string Administrador = "Administrador";
    public const string EncargadoProduccion = "EncargadoProduccion";
    public const string EncargadoCompras = "EncargadoCompras";
    public const string EncargadoVentas = "EncargadoVentas";

    private static readonly string[] AdminOnly = { Administrador };
    private static readonly string[] Produccion = { Administrador, EncargadoProduccion };
    private static readonly string[] Compras = { Administrador, EncargadoCompras };
    private static readonly string[] Ventas = { Administrador, EncargadoVentas };

    /// <summary>Registers every report policy on an <see cref="AuthorizationOptions"/>.</summary>
    public static void AddReportsAuthorizationPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(CanViewDashboard, p => p.RequireRole(AdminOnly));
        options.AddPolicy(CanViewProduccion, p => p.RequireRole(Produccion));
        options.AddPolicy(CanViewStock, p => p.RequireRole(Produccion));
        options.AddPolicy(CanViewCompras, p => p.RequireRole(Compras));
        options.AddPolicy(CanViewCtaCteProveedor, p => p.RequireRole(Compras));
        options.AddPolicy(CanViewVentas, p => p.RequireRole(Ventas));
        options.AddPolicy(CanViewCtaCteBar, p => p.RequireRole(Ventas));
        options.AddPolicy(CanViewCostos, p => p.RequireRole(AdminOnly));
        options.AddPolicy(CanViewRentabilidad, p => p.RequireRole(AdminOnly));
    }

    /// <summary>
    /// Convenience registration for the DI container: calls <c>AddAuthorization</c> and applies
    /// the report policies. Use this in Program.cs instead of the bare <c>AddAuthorization()</c>.
    /// </summary>
    public static IServiceCollection AddReportAuthorization(this IServiceCollection services)
        => services.AddAuthorization(options => options.AddReportsAuthorizationPolicies());
}

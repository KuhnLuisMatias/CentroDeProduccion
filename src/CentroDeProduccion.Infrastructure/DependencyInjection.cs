using System.Reflection;
using CentroDeProduccion.Application.Abstractions;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Abstractions.Time;
using CentroDeProduccion.Infrastructure.Persistence;
using CentroDeProduccion.Infrastructure.Persistence.Repositories;
using CentroDeProduccion.Infrastructure.Security;
using CentroDeProduccion.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CentroDeProduccion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IInsumoRepository, InsumoRepository>();
        services.AddScoped<IMovimientoStockRepository, MovimientoStockRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnidadMedidaRepository, UnidadMedidaRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRecetaRepository, RecetaRepository>();
        services.AddScoped<IProduccionRepository, ProduccionRepository>();
        services.AddScoped<IProductoTerminadoRepository, ProductoTerminadoRepository>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<IOrdenCompraRepository, OrdenCompraRepository>();
        services.AddScoped<ICuentaCorrienteProveedorRepository, CuentaCorrienteProveedorRepository>();
        services.AddScoped<IPagoProveedorRepository, PagoProveedorRepository>();
        services.AddScoped<IBarRepository, BarRepository>();
        services.AddScoped<IRemitoRepository, RemitoRepository>();
        services.AddScoped<ICuentaCorrienteBarRepository, CuentaCorrienteBarRepository>();
        services.AddScoped<IDevolucionRepository, DevolucionRepository>();
        services.AddScoped<IPagoBarRepository, PagoBarRepository>();
        services.AddScoped<IInventarioSesionRepository, InventarioSesionRepository>();

        // Security
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenHasher, TokenHasher>();
        services.AddSingleton<IClock, Clock>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Export services — Excel is the default IExportService; both concrete exporters are
        // also registered directly so callers can inject the format they need.
        services.AddScoped<IExportService, ExcelExportService>();
        services.AddScoped<ExcelExportService>();
        services.AddScoped<PdfExportService>();

        // Print template service — renders HTML templates from wwwroot/templates/print
        services.AddScoped<IPrintTemplateService, FilePrintTemplateService>();

        // FluentValidation — scan Application assembly where validators live
        var applicationAssembly = typeof(CentroDeProduccion.Application.Common.Result).Assembly;
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}

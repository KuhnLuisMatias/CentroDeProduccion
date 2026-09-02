using System.Text;
using CentroDeProduccion.Application.Features.Auth.Commands.Register;
using CentroDeProduccion.Application.Features.Auth.Commands.Bootstrap;
using CentroDeProduccion.Application.Features.Auth.Commands.Login;
using CentroDeProduccion.Application.Features.Auth.Commands.Refresh;
using CentroDeProduccion.Application.Features.Auth.Commands.ChangePassword;
using CentroDeProduccion.Application.Features.Insumos.Commands.CreateInsumo;
using CentroDeProduccion.Application.Features.Insumos.Commands.ReactivateInsumo;
using CentroDeProduccion.Application.Features.Insumos.Commands.UpdateInsumo;
using CentroDeProduccion.Application.Features.Categorias.Commands.CreateCategoria;
using CentroDeProduccion.Application.Features.Categorias.Commands.UpdateCategoria;
using CentroDeProduccion.Application.Features.Categorias.Commands.DeactivateCategoria;
using CentroDeProduccion.Application.Features.UnidadesMedida.Commands.CreateUnidadMedida;
using CentroDeProduccion.Application.Features.UnidadesMedida.Commands.UpdateUnidadMedida;
using CentroDeProduccion.Application.Features.UnidadesMedida.Commands.DeactivateUnidadMedida;
using CentroDeProduccion.Application.Features.Proveedores.Commands.CreateProveedor;
using CentroDeProduccion.Application.Features.Proveedores.Commands.UpdateProveedor;
using CentroDeProduccion.Application.Features.Stock.Commands.RegisterMovement;
using CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;
using CentroDeProduccion.Application.Features.Recetas.Commands.UpdateReceta;
using CentroDeProduccion.Application.Features.Recetas.Queries.CalcularCosto;
using CentroDeProduccion.Application.Features.Produccion.Commands.CreateProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.ConfirmProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.EditarInsumosProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.CancelProduccion;
using CentroDeProduccion.Application.Features.ProductosTerminados.Commands.CreateProductoTerminado;
using CentroDeProduccion.Application.Features.ProductosTerminados.Commands.UpdateProductoTerminado;
using CentroDeProduccion.Application.Features.ProductosTerminados.Commands.ReserveStock;
using CentroDeProduccion.Application.Features.Empleados.Commands.CreateEmpleado;
using CentroDeProduccion.Application.Features.Empleados.Commands.UpdateEmpleado;
using CentroDeProduccion.Application.Features.Empleados.Commands.DeleteEmpleado;
using CentroDeProduccion.Application.Features.Dashboard.Queries;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Reports.Produccion;
using CentroDeProduccion.Application.Features.Reports.Stock;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Application.Features.Reports.Compras;
using CentroDeProduccion.Application.Features.Reports.Ventas;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CancelarOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CreateOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.EnviarOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.GenerarOCDesdeAlertas;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.UpdateOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Queries.GetOrdenCompraById;
using CentroDeProduccion.Application.Features.OrdenesCompra.Queries.GetOrdenCompraList;
using CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaCredito;
using CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaDebito;
using CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetEstadoCuenta;
using CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetMovimientos;
using CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetSaldo;
using CentroDeProduccion.Application.Features.Pagos.Commands.CreatePagoProveedor;
using CentroDeProduccion.Application.Features.Pagos.Queries.GetPagoById;
using CentroDeProduccion.Application.Features.Pagos.Queries.GetPagoList;
using CentroDeProduccion.Application.Features.Bares.Commands.CreateBar;
using CentroDeProduccion.Application.Features.Bares.Commands.UpdateBar;
using CentroDeProduccion.Application.Features.Bares.Commands.DeleteBar;
using CentroDeProduccion.Application.Features.Bares.Commands.ReactivateBar;
using CentroDeProduccion.Application.Features.Bares.Queries.GetBarById;
using CentroDeProduccion.Application.Features.Bares.Queries.GetBarList;
using CentroDeProduccion.Application.Features.Remitos.Commands.CreateRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.UpdateRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.UpdateEstadoRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.CancelarRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.ConfirmRemito;
using CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoById;
using CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoList;
using CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoPrint;
using CentroDeProduccion.Application.Features.Remitos.Queries.GetOrdenCarga;
using CentroDeProduccion.Application.Features.Devoluciones.Commands.CreateDevolucion;
using CentroDeProduccion.Application.Features.Devoluciones.Queries.GetDevolucionById;
using CentroDeProduccion.Application.Features.Devoluciones.Queries.GetDevolucionList;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterCompensacion;
using CentroDeProduccion.Application.Features.PagosBar.Commands.CreatePagoBar;
using CentroDeProduccion.Application.Features.PagosBar.Queries.GetPagoBarById;
using CentroDeProduccion.Application.Features.PagosBar.Queries.GetPagoBarList;
using CentroDeProduccion.Application.Features.Inventario.Commands.CreateInventarioSesion;
using CentroDeProduccion.Application.Features.Inventario.Commands.RegistrarConteo;
using CentroDeProduccion.Application.Features.Inventario.Commands.ConfirmInventarioSesion;
using CentroDeProduccion.Application.Features.Inventario.Queries.GetInventarioSesionById;
using CentroDeProduccion.Application.Features.Inventario.Queries.GetInventarioSesionList;
using CentroDeProduccion.Application.Authorization;
using CentroDeProduccion.Infrastructure;
using CentroDeProduccion.Infrastructure.Data;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (DI, repos, services) ──
builder.Services.AddInfrastructure();

// ── Application handlers ──
builder.Services.AddScoped<RegisterCommandHandler>();
builder.Services.AddScoped<BootstrapCommandHandler>();
builder.Services.AddScoped<LoginCommandHandler>();
builder.Services.AddScoped<RefreshTokenCommandHandler>();
builder.Services.AddScoped<ChangePasswordCommandHandler>();
builder.Services.AddScoped<CreateInsumoCommandHandler>();
builder.Services.AddScoped<UpdateInsumoCommandHandler>();
builder.Services.AddScoped<ReactivateInsumoCommandHandler>();
builder.Services.AddScoped<CreateCategoriaCommandHandler>();
builder.Services.AddScoped<UpdateCategoriaCommandHandler>();
builder.Services.AddScoped<DeactivateCategoriaCommandHandler>();
builder.Services.AddScoped<CreateUnidadMedidaCommandHandler>();
builder.Services.AddScoped<UpdateUnidadMedidaCommandHandler>();
builder.Services.AddScoped<DeactivateUnidadMedidaCommandHandler>();
builder.Services.AddScoped<CreateProveedorCommandHandler>();
builder.Services.AddScoped<UpdateProveedorCommandHandler>();
builder.Services.AddScoped<RegisterMovementCommandHandler>();
builder.Services.AddScoped<CreateRecetaCommandHandler>();
builder.Services.AddScoped<UpdateRecetaCommandHandler>();
builder.Services.AddScoped<CalcularCostoRecetaHandler>();
builder.Services.AddScoped<CreateProduccionCommandHandler>();
builder.Services.AddScoped<ConfirmProduccionCommandHandler>();
builder.Services.AddScoped<EditarInsumosProduccionCommandHandler>();
builder.Services.AddScoped<CancelProduccionCommandHandler>();
builder.Services.AddScoped<CreateProductoTerminadoCommandHandler>();
builder.Services.AddScoped<UpdateProductoTerminadoCommandHandler>();
builder.Services.AddScoped<ReserveStockCommandHandler>();
builder.Services.AddScoped<CreateEmpleadoCommandHandler>();
builder.Services.AddScoped<UpdateEmpleadoCommandHandler>();
builder.Services.AddScoped<DeleteEmpleadoCommandHandler>();
builder.Services.AddScoped<GetProduccionPeriodoReportQueryHandler>();
builder.Services.AddScoped<GetProduccionProductoReportQueryHandler>();
builder.Services.AddScoped<GetStockInsumosValoradoReportQueryHandler>();
builder.Services.AddScoped<GetStockInsumosBajoMinimoReportQueryHandler>();
builder.Services.AddScoped<GetStockInsumosMovimientosReportQueryHandler>();
builder.Services.AddScoped<GetStockPTValoradoReportQueryHandler>();
builder.Services.AddScoped<GetStockPTProximosAVencerReportQueryHandler>();
builder.Services.AddScoped<GetStockPTMovimientosReportQueryHandler>();
builder.Services.AddScoped<GetDashboardQueryHandler>();
builder.Services.AddScoped<GetDashboardChartsQueryHandler>();
builder.Services.AddScoped<CreateOrdenCompraCommandHandler>();
builder.Services.AddScoped<UpdateOrdenCompraCommandHandler>();
builder.Services.AddScoped<EnviarOrdenCompraCommandHandler>();
builder.Services.AddScoped<CancelarOrdenCompraCommandHandler>();
builder.Services.AddScoped<GenerarOCDesdeAlertasCommandHandler>();
builder.Services.AddScoped<GetOrdenCompraByIdQueryHandler>();
builder.Services.AddScoped<GetOrdenCompraListQueryHandler>();
builder.Services.AddScoped<RegisterNotaDebitoCommandHandler>();
builder.Services.AddScoped<RegisterNotaCreditoCommandHandler>();
builder.Services.AddScoped<GetEstadoCuentaQueryHandler>();
builder.Services.AddScoped<GetMovimientosQueryHandler>();
builder.Services.AddScoped<GetSaldoQueryHandler>();
builder.Services.AddScoped<CreatePagoProveedorCommandHandler>();
builder.Services.AddScoped<GetPagoByIdQueryHandler>();
builder.Services.AddScoped<GetPagoListQueryHandler>();
builder.Services.AddScoped<CreateBarCommandHandler>();
builder.Services.AddScoped<UpdateBarCommandHandler>();
builder.Services.AddScoped<DeleteBarCommandHandler>();
builder.Services.AddScoped<ReactivateBarCommandHandler>();
builder.Services.AddScoped<GetBarByIdQueryHandler>();
builder.Services.AddScoped<GetBarListQueryHandler>();
builder.Services.AddScoped<CreateRemitoCommandHandler>();
builder.Services.AddScoped<UpdateRemitoCommandHandler>();
builder.Services.AddScoped<UpdateEstadoRemitoCommandHandler>();
builder.Services.AddScoped<CancelarRemitoCommandHandler>();
builder.Services.AddScoped<ConfirmRemitoCommandHandler>();
builder.Services.AddScoped<GetRemitoByIdQueryHandler>();
builder.Services.AddScoped<GetRemitoListQueryHandler>();
builder.Services.AddScoped<GetRemitoPrintQueryHandler>();
builder.Services.AddScoped<GetOrdenCargaQueryHandler>();
builder.Services.AddScoped<CreateDevolucionCommandHandler>();
builder.Services.AddScoped<GetDevolucionByIdQueryHandler>();
builder.Services.AddScoped<GetDevolucionListQueryHandler>();
builder.Services.AddScoped<CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries.GetSaldo.GetSaldoQueryHandler>();
builder.Services.AddScoped<CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries.GetEstadoCuenta.GetEstadoCuentaQueryHandler>();
builder.Services.AddScoped<CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaCredito.RegisterNotaCreditoCommandHandler>();
builder.Services.AddScoped<CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaDebito.RegisterNotaDebitoCommandHandler>();
builder.Services.AddScoped<RegisterCompensacionCommandHandler>();
builder.Services.AddScoped<CreatePagoBarCommandHandler>();
builder.Services.AddScoped<GetPagoBarByIdQueryHandler>();
builder.Services.AddScoped<GetPagoBarListQueryHandler>();
builder.Services.AddScoped<CreateInventarioSesionCommandHandler>();
builder.Services.AddScoped<RegistrarConteoCommandHandler>();
builder.Services.AddScoped<ConfirmInventarioSesionCommandHandler>();
builder.Services.AddScoped<GetInventarioSesionByIdQueryHandler>();
builder.Services.AddScoped<GetInventarioSesionListQueryHandler>();

// ── Reportes y dashboard: Compras y Ventas ──
builder.Services.AddScoped<GetComprasPorProveedorReportQueryHandler>();
builder.Services.AddScoped<GetEvolucionPreciosReportQueryHandler>();
builder.Services.AddScoped<GetCtaCteProveedorReportQueryHandler>();
builder.Services.AddScoped<GetResumenProveedoresReportQueryHandler>();
builder.Services.AddScoped<GetVentasPorBarReportQueryHandler>();
builder.Services.AddScoped<GetVentasPeriodoReportQueryHandler>();
builder.Services.AddScoped<GetCtaCteBarReportQueryHandler>();
builder.Services.AddScoped<GetDevolucionesReportQueryHandler>();

// ── Reportes y dashboard: Costos/Rentabilidad ──
builder.Services.AddScoped<RecetaCostoResolver>();
builder.Services.AddScoped<ProductoTerminadoCostoResolver>();
builder.Services.AddScoped<GetCostoProductoReportQueryHandler>();
builder.Services.AddScoped<GetRentabilidadProductoReportQueryHandler>();
builder.Services.AddScoped<GetRentabilidadBarReportQueryHandler>();
builder.Services.AddScoped<GetPlanillaCostosReportQueryHandler>();
builder.Services.AddScoped<GetPedidosDetalleReportQueryHandler>();
builder.Services.AddScoped<GetMatrizSemanalReportQueryHandler>();

// ── Database ──
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Auth (JWT) ──
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddReportAuthorization();

// ── Controllers + OpenAPI ──
// IgnoreCycles: endpoints that serialize EF entity graphs (bidirectional navigations such as
// Categoria↔Insumo or Produccion↔Salidas) must not crash with JSON cycle exceptions.
// NOTE: on .NET 10 the MVC JsonOptions property is JsonSerializerOptions (was SerializerOptions).
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddOpenApi();

// ── Problem Details (RFC 7807) for unhandled exceptions; expected failures are mapped by
//    Api/Extensions/ResultExtensions and never reach this handler. See design D5. ──
builder.Services.AddProblemDetails();

// ── CORS (para el frontend Next.js) ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.SetIsOriginAllowed(origin =>
                   origin.StartsWith("http://localhost") ||
                   System.Text.RegularExpressions.Regex.IsMatch(origin,
                       @"^http://(192\.168|10\.|172\.(1[6-9]|2\d|3[01])|100\.(6[4-9]|[7-9]\d|1[01]\d|12[0-7]))\.\d{1,3}\.\d{1,3}(:\d+)?$"))
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ── Middleware ──
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseStaticFiles();
app.UseCors("DevCors");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseMiddleware<CentroDeProduccion.Api.Middleware.DebeCambiarPasswordMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

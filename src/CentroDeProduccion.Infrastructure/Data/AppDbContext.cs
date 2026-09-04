using CentroDeProduccion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Insumo> Insumos => Set<Insumo>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<Receta> Recetas => Set<Receta>();
    public DbSet<RecetaInsumo> RecetaInsumos => Set<RecetaInsumo>();
    public DbSet<RecetaVersion> RecetaVersiones => Set<RecetaVersion>();
    public DbSet<ProductoTerminado> ProductosTerminados => Set<ProductoTerminado>();
    public DbSet<Produccion> Producciones => Set<Produccion>();
    public DbSet<ProduccionSalida> ProduccionSalidas => Set<ProduccionSalida>();
    public DbSet<ProduccionInsumo> ProduccionInsumos => Set<ProduccionInsumo>();
    public DbSet<PresentacionVenta> PresentacionesVenta => Set<PresentacionVenta>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<OrdenCompraItem> OrdenCompraItems => Set<OrdenCompraItem>();
    public DbSet<CuentaCorrienteProveedor> CuentasCorrientesProveedores => Set<CuentaCorrienteProveedor>();
    public DbSet<PagoProveedor> PagosProveedor => Set<PagoProveedor>();

    public DbSet<Bar> Bares => Set<Bar>();
    public DbSet<Remito> Remitos => Set<Remito>();
    public DbSet<RemitoLinea> RemitoLineas => Set<RemitoLinea>();
    public DbSet<CuentaCorrienteBar> CuentasCorrientesBar => Set<CuentaCorrienteBar>();
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<DevolucionLinea> DevolucionLineas => Set<DevolucionLinea>();
    public DbSet<PagoBar> PagosBar => Set<PagoBar>();
    public DbSet<PagoBarItem> PagosBarItems => Set<PagoBarItem>();
    public DbSet<InventarioSesion> InventarioSesiones => Set<InventarioSesion>();
    public DbSet<InventarioConteo> InventarioConteos => Set<InventarioConteo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Usuario
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Rol).HasConversion<int>();
        });

        // RefreshToken (design D4)
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(rt => rt.TokenHash).IsUnique();
            e.HasOne(rt => rt.Usuario)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(rt => rt.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rt => rt.ReemplazadoPor)
             .WithMany()
             .HasForeignKey(rt => rt.ReemplazadoPorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Categoria (design D8: uniqueness scoped to (Ambito, Nombre))
        modelBuilder.Entity<Categoria>(e =>
        {
            e.Property(c => c.Ambito).HasConversion<int>();
            e.HasIndex(c => new { c.Ambito, c.Nombre }).IsUnique();
        });

        // UnidadMedida (design D6)
        modelBuilder.Entity<UnidadMedida>(e =>
        {
            e.Property(u => u.Tipo).HasConversion<int>();
        });

        // Insumo (design D6, D7)
        modelBuilder.Entity<Insumo>(e =>
        {
            e.HasIndex(i => i.CodigoSku).IsUnique();
            e.Property(i => i.StockMinimo).HasPrecision(18, 4);
            e.Property(i => i.StockActual).HasPrecision(18, 4);
            e.Property(i => i.PrecioUltimaCompra).HasPrecision(18, 4);
            e.Property(i => i.FactorConversion).HasPrecision(18, 6);
            e.Property(i => i.RowVersion).IsRowVersion();
            e.HasOne(i => i.Categoria)
             .WithMany(c => c.Insumos)
             .HasForeignKey(i => i.CategoriaId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.ProveedorPrincipal)
             .WithMany(p => p.Insumos)
             .HasForeignKey(i => i.ProveedorPrincipalId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.UnidadCompra)
             .WithMany()
             .HasForeignKey(i => i.UnidadCompraId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.UnidadConsumo)
             .WithMany()
             .HasForeignKey(i => i.UnidadConsumoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Proveedor
        modelBuilder.Entity<Proveedor>(e =>
        {
            e.HasIndex(p => p.Cuit).IsUnique();
        });

        // MovimientoStock (design D6, Phase 2: dual insumo/producto-terminado target)
        modelBuilder.Entity<MovimientoStock>(e =>
        {
            e.Property(m => m.Tipo).HasConversion<int>();
            e.Property(m => m.Cantidad).HasPrecision(18, 4);
            e.Property(m => m.CantidadOriginal).HasPrecision(18, 4);
            e.Property(m => m.FactorConversionAplicado).HasPrecision(18, 6);
            e.Property(m => m.PrecioUnitario).HasPrecision(18, 4);
            e.HasOne(m => m.Insumo)
             .WithMany(i => i.Movimientos)
             .HasForeignKey(m => m.InsumoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.ProductoTerminado)
             .WithMany()
             .HasForeignKey(m => m.ProductoTerminadoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Produccion)
             .WithMany()
             .HasForeignKey(m => m.ProduccionId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Usuario)
             .WithMany()
             .HasForeignKey(m => m.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.UnidadOriginal)
             .WithMany()
             .HasForeignKey(m => m.UnidadOriginalId)
             .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_MovimientosStock_UnSoloTarget",
                "([InsumoId] IS NOT NULL AND [ProductoTerminadoId] IS NULL) OR ([InsumoId] IS NULL AND [ProductoTerminadoId] IS NOT NULL)"));
        });

        // Receta (Phase 2)
        modelBuilder.Entity<Receta>(e =>
        {
            e.HasIndex(r => r.CodigoSku).IsUnique();
            e.Property(r => r.Estado).HasConversion<int>();
            e.Property(r => r.RowVersion).IsRowVersion();
            e.HasOne(r => r.Categoria)
             .WithMany()
             .HasForeignKey(r => r.CategoriaId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.UnidadMedida)
             .WithMany()
             .HasForeignKey(r => r.UnidadMedidaId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // RecetaVersion (Phase 2, snapshot versioning §3.5)
        modelBuilder.Entity<RecetaVersion>(e =>
        {
            e.Property(rv => rv.Version).IsRequired();
            e.HasOne(rv => rv.Receta)
             .WithMany(r => r.Versiones)
             .HasForeignKey(rv => rv.RecetaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(rv => new { rv.RecetaId, rv.Version }).IsUnique();
        });

        // RecetaInsumo (Phase 2, BOM: insumo OR sub-recipe)
        modelBuilder.Entity<RecetaInsumo>(e =>
        {
            e.Property(ri => ri.CantidadNecesaria).HasPrecision(18, 4);
            e.HasOne(ri => ri.Receta)
             .WithMany(r => r.Insumos)
             .HasForeignKey(ri => ri.RecetaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ri => ri.Insumo)
             .WithMany()
             .HasForeignKey(ri => ri.InsumoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ri => ri.RecetaOrigen)
             .WithMany()
             .HasForeignKey(ri => ri.RecetaOrigenId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ri => ri.UnidadMedida)
             .WithMany()
             .HasForeignKey(ri => ri.UnidadMedidaId)
             .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_RecetaInsumos_UnSoloOrigen",
                "([InsumoId] IS NOT NULL AND [RecetaOrigenId] IS NULL) OR ([InsumoId] IS NULL AND [RecetaOrigenId] IS NOT NULL)"));
        });

        // ProductoTerminado (Phase 2)
        modelBuilder.Entity<ProductoTerminado>(e =>
        {
            e.HasIndex(pt => pt.CodigoSku).IsUnique();
            e.Property(pt => pt.StockActual).HasPrecision(18, 4);
            e.Property(pt => pt.StockMinimo).HasPrecision(18, 4);
            e.Property(pt => pt.Estado).HasConversion<int>();
            e.Property(pt => pt.RowVersion).IsRowVersion();
            e.HasOne(pt => pt.Categoria)
             .WithMany()
             .HasForeignKey(pt => pt.CategoriaId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(pt => pt.UnidadMedida)
             .WithMany()
             .HasForeignKey(pt => pt.UnidadMedidaId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(pt => pt.Receta)
             .WithMany()
             .HasForeignKey(pt => pt.RecetaId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Produccion (Phase 2)
        modelBuilder.Entity<Produccion>(e =>
        {
            e.Property(p => p.Estado).HasConversion<int>();
            e.Property(p => p.CantidadProducida).HasPrecision(18, 4);
            e.Property(p => p.CostoTotalInsumos).HasPrecision(18, 4);
            e.Property(p => p.CostoTotal).HasPrecision(18, 4);
            e.Property(p => p.RowVersion).IsRowVersion();
            e.HasOne(p => p.Receta)
             .WithMany()
             .HasForeignKey(p => p.RecetaId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Responsable)
             .WithMany()
             .HasForeignKey(p => p.ResponsableId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => new { p.Fecha, p.RecetaId });
        });

        // ProduccionSalida (Phase 2)
        modelBuilder.Entity<ProduccionSalida>(e =>
        {
            e.Property(ps => ps.Cantidad).HasPrecision(18, 4);
            e.Property(ps => ps.TipoSalida).HasConversion<int>();
            e.HasOne(ps => ps.Produccion)
             .WithMany(p => p.Salidas)
             .HasForeignKey(ps => ps.ProduccionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ps => ps.ProductoTerminado)
             .WithMany()
             .HasForeignKey(ps => ps.ProductoTerminadoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ProduccionInsumo (Fase 8, producción simple: editable consumption lines)
        modelBuilder.Entity<ProduccionInsumo>(e =>
        {
            e.Property(pi => pi.Cantidad).HasPrecision(18, 4);
            e.Property(pi => pi.Observaciones).HasMaxLength(500);
            e.HasOne(pi => pi.Produccion)
             .WithMany(p => p.InsumosConsumidos)
             .HasForeignKey(pi => pi.ProduccionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(pi => pi.Insumo)
             .WithMany()
             .HasForeignKey(pi => pi.InsumoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(pi => pi.ProduccionId);
        });

        // PresentacionVenta (Phase 2)
        modelBuilder.Entity<PresentacionVenta>(e =>
        {
            e.Property(pv => pv.Cantidad).HasPrecision(18, 4);
            e.HasOne(pv => pv.Receta)
             .WithMany(r => r.Presentaciones)
             .HasForeignKey(pv => pv.RecetaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(pv => pv.UnidadMedida)
             .WithMany()
             .HasForeignKey(pv => pv.UnidadMedidaId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Empleado (Phase 3)
        modelBuilder.Entity<Empleado>(e =>
        {
            e.HasIndex(em => em.Dni).IsUnique();
            e.Property(em => em.TarifaPorHora).HasPrecision(18, 4);
            e.Property(em => em.Cargo).HasConversion<int>();
            e.Property(em => em.Categoria).HasConversion<int>();
            e.Property(em => em.RowVersion).IsRowVersion();
        });

        // RegistroMerma (Phase 3) — removed (module deleted, Fase9)

        // OrdenCompra (Phase 4)
        modelBuilder.Entity<OrdenCompra>(e =>
        {
            e.HasIndex(oc => oc.Numero).IsUnique();
            e.Property(oc => oc.Estado).HasConversion<int>();
            e.Property(oc => oc.RowVersion).IsRowVersion();
            e.HasOne(oc => oc.Proveedor)
             .WithMany()
             .HasForeignKey(oc => oc.ProveedorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(oc => new { oc.FechaCreacion, oc.Estado, oc.ProveedorId });
        });

        // OrdenCompraItem (Phase 4)
        modelBuilder.Entity<OrdenCompraItem>(e =>
        {
            e.Property(oci => oci.CantidadPedida).HasPrecision(18, 4);
            e.Property(oci => oci.PrecioUnitario).HasPrecision(18, 4);
            e.HasOne(oci => oci.OrdenCompra)
             .WithMany(oc => oc.Items)
             .HasForeignKey(oci => oci.OrdenCompraId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(oci => oci.Insumo)
             .WithMany()
             .HasForeignKey(oci => oci.InsumoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // CuentaCorrienteProveedor (Phase 4, append-only ledger)
        modelBuilder.Entity<CuentaCorrienteProveedor>(e =>
        {
            e.Property(cc => cc.TipoMovimiento).HasConversion<int>();
            e.Property(cc => cc.Monto).HasPrecision(18, 4);
            e.HasOne(cc => cc.Proveedor)
             .WithMany()
             .HasForeignKey(cc => cc.ProveedorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(cc => cc.OrdenCompra)
             .WithMany()
             .HasForeignKey(cc => cc.OrdenCompraId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(cc => cc.PagoProveedor)
             .WithMany()
             .HasForeignKey(cc => cc.PagoProveedorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_CuentaCorriente_MontoSigno",
                "([TipoMovimiento] IN (1,3) AND [Monto] > 0) OR ([TipoMovimiento] IN (2,4) AND [Monto] < 0)"));
        });

        // PagoProveedor (Phase 4)
        modelBuilder.Entity<PagoProveedor>(e =>
        {
            e.HasIndex(pp => pp.Numero).IsUnique();
            e.Property(pp => pp.MontoTotal).HasPrecision(18, 4);
            e.HasOne(pp => pp.Proveedor)
             .WithMany()
             .HasForeignKey(pp => pp.ProveedorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.OwnsMany(pp => pp.Metodos, metodo =>
            {
                metodo.Property(m => m.Tipo).HasConversion<int>();
                metodo.Property(m => m.Monto).HasPrecision(18, 4);
                metodo.Property(m => m.Referencia).HasMaxLength(100);
            });

            e.OwnsMany(pp => pp.Insumos, insumo =>
            {
                insumo.Property(pi => pi.Cantidad).HasPrecision(18, 4);
                insumo.Property(pi => pi.PrecioUnitario).HasPrecision(18, 4);
                insumo.HasIndex(pi => pi.InsumoId);
                insumo.HasOne(pi => pi.Insumo)
                 .WithMany()
                 .HasForeignKey(pi => pi.InsumoId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        });

        // Cheque (Phase 4) — removed (module deleted, Fase9)

        // Bar (Phase 5)
        modelBuilder.Entity<Bar>(e =>
        {
            e.HasIndex(b => b.Nombre).IsUnique();
            e.Property(b => b.MargenReventaPorcentaje).HasPrecision(18, 4);
            e.Property(b => b.Estado).HasConversion<int>();
            e.Property(b => b.RowVersion).IsRowVersion();
        });

        // Remito (Phase 5)
        modelBuilder.Entity<Remito>(e =>
        {
            e.HasIndex(r => r.NumeroRemito).IsUnique();
            e.Property(r => r.Estado).HasConversion<int>();
            e.Property(r => r.RowVersion).IsRowVersion();
            e.HasOne(r => r.Bar)
             .WithMany()
             .HasForeignKey(r => r.BarId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(r => r.Lineas)
             .WithOne(rl => rl.Remito)
             .HasForeignKey(rl => rl.RemitoId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => new { r.Fecha, r.Estado, r.BarId });
        });

        // RemitoLinea (Phase 5, dual producto-terminado/insumo target)
        modelBuilder.Entity<RemitoLinea>(e =>
        {
            e.Property(rl => rl.Cantidad).HasPrecision(18, 4);
            e.Property(rl => rl.PrecioUnitario).HasPrecision(18, 4);
            e.Property(rl => rl.Subtotal).HasPrecision(18, 4);
            e.Property(rl => rl.TipoLinea).HasConversion<int>();
            e.Property(rl => rl.Observaciones).HasMaxLength(500);
            e.HasOne(rl => rl.Remito)
             .WithMany(r => r.Lineas)
             .HasForeignKey(rl => rl.RemitoId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rl => rl.ProductoTerminado)
             .WithMany()
             .HasForeignKey(rl => rl.ProductoTerminadoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(rl => rl.Insumo)
             .WithMany()
             .HasForeignKey(rl => rl.InsumoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_RemitoLinea_UnSoloTarget",
                "([ProductoTerminadoId] IS NOT NULL AND [InsumoId] IS NULL) OR ([ProductoTerminadoId] IS NULL AND [InsumoId] IS NOT NULL)"));
        });

        // CuentaCorrienteBar (Phase 5, append-only ledger)
        modelBuilder.Entity<CuentaCorrienteBar>(e =>
        {
            e.Property(cc => cc.Monto).HasPrecision(18, 4);
            e.Property(cc => cc.TipoMovimiento).HasConversion<int>();
            e.HasOne(cc => cc.Bar)
             .WithMany()
             .HasForeignKey(cc => cc.BarId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(cc => cc.Remito)
             .WithMany()
             .HasForeignKey(cc => cc.RemitoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(cc => cc.Devolucion)
             .WithMany()
             .HasForeignKey(cc => cc.DevolucionId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(cc => cc.PagoBar)
             .WithMany()
             .HasForeignKey(cc => cc.PagoBarId)
             .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_CuentaCorrienteBar_MontoSigno",
                "([TipoMovimiento] IN (1,4,6) AND [Monto] > 0) OR ([TipoMovimiento] IN (2,3,5) AND [Monto] < 0)"));
        });

        // Devolucion (Phase 5)
        modelBuilder.Entity<Devolucion>(e =>
        {
            e.HasIndex(d => d.Numero).IsUnique();
            e.HasOne(d => d.Remito)
             .WithMany()
             .HasForeignKey(d => d.RemitoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(d => d.Lineas)
             .WithOne(dl => dl.Devolucion)
             .HasForeignKey(dl => dl.DevolucionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // DevolucionLinea (Phase 5)
        modelBuilder.Entity<DevolucionLinea>(e =>
        {
            e.Property(dl => dl.Cantidad).HasPrecision(18, 4);
            e.HasOne(dl => dl.ProductoTerminado)
             .WithMany()
             .HasForeignKey(dl => dl.ProductoTerminadoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PagoBar (Phase 5)
        modelBuilder.Entity<PagoBar>(e =>
        {
            e.HasIndex(pb => pb.Numero).IsUnique();
            e.Property(pb => pb.MontoTotal).HasPrecision(18, 4);
            e.Property(pb => pb.RowVersion).IsRowVersion();
            e.HasOne(pb => pb.Bar)
             .WithMany()
             .HasForeignKey(pb => pb.BarId)
             .OnDelete(DeleteBehavior.Restrict);
            e.OwnsMany(pb => pb.Metodos, metodo =>
            {
                metodo.Property(m => m.Tipo).HasConversion<int>();
                metodo.Property(m => m.Monto).HasPrecision(18, 4);
            });
            e.HasMany(pb => pb.Items)
             .WithOne(pbi => pbi.PagoBar)
             .HasForeignKey(pbi => pbi.PagoBarId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PagoBarItem (Phase 5, multi-remito allocation)
        modelBuilder.Entity<PagoBarItem>(e =>
        {
            e.Property(pbi => pbi.MontoAplicado).HasPrecision(18, 4);
            e.HasOne(pbi => pbi.Remito)
             .WithMany()
             .HasForeignKey(pbi => pbi.RemitoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // InventarioSesion (Fase 7, guided inventory)
        modelBuilder.Entity<InventarioSesion>(e =>
        {
            e.Property(i => i.TipoInventario).HasConversion<int>();
            e.Property(i => i.Estado).HasConversion<int>();
            e.Property(i => i.RowVersion).IsRowVersion();
            e.HasMany(i => i.Conteos)
             .WithOne(c => c.InventarioSesion)
             .HasForeignKey(c => c.InventarioSesionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(i => new { i.Fecha, i.Estado });
        });

        // InventarioConteo (Fase 7, dual insumo/producto-terminado target)
        modelBuilder.Entity<InventarioConteo>(e =>
        {
            e.Property(c => c.CantidadSistema).HasPrecision(18, 4);
            e.Property(c => c.CantidadContada).HasPrecision(18, 4);
            e.HasOne(c => c.Insumo)
             .WithMany()
             .HasForeignKey(c => c.InsumoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.ProductoTerminado)
             .WithMany()
             .HasForeignKey(c => c.ProductoTerminadoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint(
                "CK_InventarioConteo_UnSoloTarget",
                "([InsumoId] IS NOT NULL AND [ProductoTerminadoId] IS NULL) OR ([InsumoId] IS NULL AND [ProductoTerminadoId] IS NOT NULL)"));
        });
    }
}

using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Domain.Entities;
using NSubstitute;
using Shouldly;
using RecetaEntity = CentroDeProduccion.Domain.Entities.Receta;

namespace CentroDeProduccion.Tests.Application.Reports;

/// <summary>
/// Verifies the PT cost policy: a finished product's cost is its last confirmed production's
/// real unit cost (CostoTotal / CantidadProducida) when available, NOT its live BOM estimate;
/// a recipe never confirmed in production falls back to the live BOM estimate.
/// </summary>
public class ProductoTerminadoCostoResolverTests
{
    private readonly IProduccionRepository _produccionRepository = Substitute.For<IProduccionRepository>();
    private readonly IRecetaRepository _recetaRepository = Substitute.For<IRecetaRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();

    private ProductoTerminadoCostoResolver CreateResolver()
        => new(_recetaRepository, _produccionRepository, new RecetaCostoResolver(_recetaRepository, _insumoRepository, _produccionRepository));

    /// <summary>Recipe of 1 harina x $10 → live BOM estimate $10, deliberately tiny so the
    /// real-cost path is unmissable.</summary>
    private static (RecetaEntity Receta, Insumo Harina) CrearReceta()
    {
        var harina = new Insumo { Id = Guid.NewGuid(), Nombre = "Harina", PrecioUltimaCompra = 10m };
        var receta = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Tarta", CodigoSku = "REC-TARTA" };
        receta.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = receta.Id, InsumoId = harina.Id, CantidadNecesaria = 1m });
        return (receta, harina);
    }

    private void StubReceta(RecetaEntity receta, Insumo harina)
    {
        _recetaRepository.GetByIdWithDetallesAsync(receta.Id, Arg.Any<CancellationToken>()).Returns(receta);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { harina });
    }

    [Fact]
    public async Task CalcularPorRecetaAsync_RecetaConProduccionConfirmada_UsaCostoRealNoBom()
    {
        var (receta, harina) = CrearReceta();
        StubReceta(receta, harina);

        // Last confirmed production: 1 unit at 1760 total → $1760 real unit cost.
        _produccionRepository.GetLastConfirmedUnitCostsByRecetaAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(receta.Id)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal> { [receta.Id] = 1760m });

        var costo = await CreateResolver().CalcularPorRecetaAsync(receta.Id);

        // BOM estimate would be 1 x 10 = 10.
        costo.ShouldBe(1760m);
    }

    [Fact]
    public async Task CalcularPorRecetaAsync_RecetaSinProducciones_CaeAlBom()
    {
        var (receta, harina) = CrearReceta();
        StubReceta(receta, harina);

        _produccionRepository.GetLastConfirmedUnitCostsByRecetaAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(receta.Id)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>()); // never confirmed

        var costo = await CreateResolver().CalcularPorRecetaAsync(receta.Id);

        // Fallback live BOM: 1 x 10 = 10.
        costo.ShouldBe(10m);
    }

    [Fact]
    public async Task CalcularPorRecetasAsync_MezclaRealYBom_DevuelveAmbos()
    {
        var (conProduccion, harinaConProduccion) = CrearReceta();
        StubReceta(conProduccion, harinaConProduccion);

        var (soloBom, harinaSoloBom) = CrearReceta();
        StubReceta(soloBom, harinaSoloBom);

        _produccionRepository.GetLastConfirmedUnitCostsByRecetaAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(conProduccion.Id) && ids.Contains(soloBom.Id)),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal> { [conProduccion.Id] = 1760m }); // soloBom never confirmed

        var costos = await CreateResolver().CalcularPorRecetasAsync(new Guid?[] { conProduccion.Id, soloBom.Id });

        costos.Count.ShouldBe(2);
        costos[conProduccion.Id].ShouldBe(1760m);
        costos[soloBom.Id].ShouldBe(10m);
    }
}

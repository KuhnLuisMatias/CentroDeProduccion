using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Domain.Entities;
using NSubstitute;
using Shouldly;
using RecetaEntity = CentroDeProduccion.Domain.Entities.Receta;

namespace CentroDeProduccion.Tests.Application.Reports;

/// <summary>
/// Verifies the standard-cost policy for sub-recipes: a sub-recipe with at least one confirmed
/// production is priced at that run's real unit cost (CostoTotal / CantidadProducida), NOT its
/// live BOM estimate; a sub-recipe never confirmed falls back to the live BOM recursion.
/// </summary>
public class RecetaCostoResolverTests
{
    private readonly IProduccionRepository _produccionRepository = Substitute.For<IProduccionRepository>();
    private readonly IRecetaRepository _recetaRepository = Substitute.For<IRecetaRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();

    private RecetaCostoResolver CreateResolver()
        => new(_recetaRepository, _insumoRepository, _produccionRepository);

    /// <summary>Pan: 1 harina x $10 + 3 batches of Masa. Masa BOM: 2 mantequilla x $1000 per
    /// batch → BOM estimate $6000, deliberately absurd so the real-cost path is unmissable.</summary>
    private static (RecetaEntity Pan, RecetaEntity Masa, Insumo Harina, Insumo Mantequilla) CrearArbol()
    {
        var harina = new Insumo { Id = Guid.NewGuid(), Nombre = "Harina", PrecioUltimaCompra = 10m };
        var mantequilla = new Insumo { Id = Guid.NewGuid(), Nombre = "Mantequilla", PrecioUltimaCompra = 1000m };

        var masa = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Masa base", CodigoSku = "REC-MASA" };
        masa.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = masa.Id, InsumoId = mantequilla.Id, CantidadNecesaria = 2m });

        var pan = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Pan", CodigoSku = "REC-PAN" };
        pan.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = pan.Id, InsumoId = harina.Id, CantidadNecesaria = 1m });
        pan.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = pan.Id, RecetaOrigenId = masa.Id, CantidadNecesaria = 3m });

        return (pan, masa, harina, mantequilla);
    }

    private void StubArbol(RecetaEntity pan, RecetaEntity masa, Insumo harina, Insumo mantequilla)
    {
        _recetaRepository.GetByIdWithDetallesAsync(masa.Id, Arg.Any<CancellationToken>()).Returns(masa);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { harina, mantequilla });
    }

    [Fact]
    public async Task CalcularAsync_SubRecetaConProduccionConfirmada_UsaCostoRealNoBom()
    {
        var (pan, masa, harina, mantequilla) = CrearArbol();
        StubArbol(pan, masa, harina, mantequilla);

        // Last confirmed production of Masa: 500 units at 50000 total → $100 real unit cost.
        _produccionRepository.GetLastConfirmedUnitCostsByRecetaAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(masa.Id)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal> { [masa.Id] = 100m });

        var resultado = await CreateResolver().CalcularAsync(pan);

        // 1 x 10 + 3 x 100 (real) = 310 — BOM estimate would be 10 + 3 x 2000 = 6010.
        resultado.CostoUnitario.ShouldBe(310m);
        resultado.CicloDetectado.ShouldBeFalse();
    }

    [Fact]
    public async Task CalcularAsync_SubRecetaSinProducciones_CaeAlBom()
    {
        var (pan, masa, harina, mantequilla) = CrearArbol();
        StubArbol(pan, masa, harina, mantequilla);

        _produccionRepository.GetLastConfirmedUnitCostsByRecetaAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(masa.Id)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>()); // masa never confirmed

        var resultado = await CreateResolver().CalcularAsync(pan);

        // Fallback live BOM: 1 x 10 + 3 x (2 x 1000) = 6010.
        resultado.CostoUnitario.ShouldBe(6010m);
        resultado.CicloDetectado.ShouldBeFalse();
    }
}

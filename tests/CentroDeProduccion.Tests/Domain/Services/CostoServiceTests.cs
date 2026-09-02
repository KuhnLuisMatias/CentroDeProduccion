using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Services;
using Shouldly;

namespace CentroDeProduccion.Tests.Domain.Services;

/// <summary>
/// Verifies BOM cost resolution: direct insumos, sub-recipe recursion, and cycle detection.
/// Cost is always the batch total of insumos (no yield, no waste).
/// </summary>
public class CostoServiceTests
{
    private static Receta CrearReceta(Guid id) => new()
    {
        Id = id,
        Nombre = $"Receta {id.ToString()[..4]}"
    };

    private static RecetaInsumo Insumo(Guid insumoId, decimal cantidad) => new()
    {
        InsumoId = insumoId,
        CantidadNecesaria = cantidad
    };

    private static RecetaInsumo SubReceta(Guid recetaOrigenId, decimal cantidad) => new()
    {
        RecetaOrigenId = recetaOrigenId,
        CantidadNecesaria = cantidad
    };

    [Fact]
    public void Explosionar_ConsumptionUnitLine_UsesQuantityAsIs()
    {
        var unidadKg = Guid.NewGuid();
        var insumoId = Guid.NewGuid();
        var receta = CrearReceta(Guid.NewGuid());
        receta.Insumos.Add(new RecetaInsumo { InsumoId = insumoId, CantidadNecesaria = 3m, UnidadMedidaId = unidadKg });

        var insumo = new Insumo
        {
            Id = insumoId,
            Nombre = "Harina",
            UnidadCompraId = Guid.NewGuid(),
            UnidadConsumoId = unidadKg,
            FactorConversion = 25m // irrelevant: line is already in the consumption unit
        };

        var resultado = CostoService.ExplosionarInsumos(receta, _ => null, id => id == insumoId ? insumo : null);

        resultado[insumoId].ShouldBe(3m);
    }

    [Fact]
    public void Explosionar_PurchaseUnitLine_ConvertsByFactorConversion()
    {
        // Recipe line written in "Caja" (purchase unit); 1 Caja = 10 Kg (consumption unit).
        var unidadCaja = Guid.NewGuid();
        var unidadKg = Guid.NewGuid();
        var insumoId = Guid.NewGuid();
        var receta = CrearReceta(Guid.NewGuid());
        receta.Insumos.Add(new RecetaInsumo { InsumoId = insumoId, CantidadNecesaria = 1m, UnidadMedidaId = unidadCaja });

        var insumo = new Insumo
        {
            Id = insumoId,
            Nombre = "Harina",
            UnidadCompraId = unidadCaja,
            UnidadConsumoId = unidadKg,
            FactorConversion = 10m
        };

        var resultado = CostoService.ExplosionarInsumos(receta, _ => null, id => id == insumoId ? insumo : null);

        resultado[insumoId].ShouldBe(10m); // 1 Caja = 10 Kg, not 1
    }

    [Fact]
    public void Explosionar_SubRecipeLines_ConvertEachDirectLine()
    {
        // Sub-recipe consumes 1 Caja of harina (= 10 Kg) per batch; parent uses 2 batches.
        var unidadCaja = Guid.NewGuid();
        var unidadKg = Guid.NewGuid();
        var harinaId = Guid.NewGuid();
        var harina = new Insumo
        {
            Id = harinaId,
            Nombre = "Harina",
            UnidadCompraId = unidadCaja,
            UnidadConsumoId = unidadKg,
            FactorConversion = 10m
        };
        var masa = CrearReceta(Guid.NewGuid());
        masa.Insumos.Add(new RecetaInsumo { InsumoId = harinaId, CantidadNecesaria = 1m, UnidadMedidaId = unidadCaja });
        var pizza = CrearReceta(Guid.NewGuid());
        pizza.Insumos.Add(SubReceta(masa.Id, 2m));

        var recetas = new Dictionary<Guid, Receta> { [masa.Id] = masa };
        var resultado = CostoService.ExplosionarInsumos(
            pizza, id => recetas.GetValueOrDefault(id), id => id == harinaId ? harina : null);

        resultado[harinaId].ShouldBe(20m); // 2 batches x (1 Caja x 10)
    }

    [Fact]
    public void Explosionar_IncompatibleUnit_ThrowsWithInsumoName()
    {
        var insumoId = Guid.NewGuid();
        var receta = CrearReceta(Guid.NewGuid());
        receta.Insumos.Add(new RecetaInsumo { InsumoId = insumoId, CantidadNecesaria = 1m, UnidadMedidaId = Guid.NewGuid() });

        var insumo = new Insumo
        {
            Id = insumoId,
            Nombre = "Harina 000",
            UnidadCompraId = Guid.NewGuid(),
            UnidadConsumoId = Guid.NewGuid(),
            FactorConversion = 10m
        };

        var ex = Should.Throw<InvalidOperationException>(() =>
            CostoService.ExplosionarInsumos(receta, _ => null, id => id == insumoId ? insumo : null));

        ex.Message.ShouldContain("La unidad de la línea de receta no coincide con las unidades del insumo");
        ex.Message.ShouldContain(insumo.Nombre);
    }

    [Fact]
    public void Calcular_DirectInsumos_SumsPriceTimesQuantity()
    {
        var insumoA = Guid.NewGuid();
        var insumoB = Guid.NewGuid();
        var receta = CrearReceta(Guid.NewGuid());
        receta.Insumos.Add(Insumo(insumoA, 2m));
        receta.Insumos.Add(Insumo(insumoB, 3m));

        var precios = new Dictionary<Guid, decimal> { [insumoA] = 100m, [insumoB] = 50m };
        var resultado = CostoService.Calcular(receta, _ => null, id => precios[id]);

        // 2x100 + 3x50 = 350; batch cost = unit cost (no yield)
        resultado.CostoInsumos.ShouldBe(350m);
        resultado.CostoUnitario.ShouldBe(350m);
        resultado.CicloDetectado.ShouldBeFalse();
    }

    [Fact]
    public void Calcular_SubReceta_ResolvesInDepth()
    {
        // "Masa" (sub): one batch = 2 harina x $10 = $20
        var harina = Guid.NewGuid();
        var masa = CrearReceta(Guid.NewGuid());
        masa.Insumos.Add(Insumo(harina, 2m));

        // "Pizza": uses 3 batches of masa (sub) + 1 queso x $30
        var queso = Guid.NewGuid();
        var pizza = CrearReceta(Guid.NewGuid());
        pizza.Insumos.Add(SubReceta(masa.Id, 3m));
        pizza.Insumos.Add(Insumo(queso, 1m));

        var precios = new Dictionary<Guid, decimal> { [harina] = 10m, [queso] = 30m };
        var recetas = new Dictionary<Guid, Receta> { [masa.Id] = masa };
        var resultado = CostoService.Calcular(pizza, id => recetas.GetValueOrDefault(id), id => precios[id]);

        // Masa lote = 20; 3 batches x 20 = 60; queso = 30; total = 90
        resultado.CostoInsumos.ShouldBe(90m);
        resultado.CicloDetectado.ShouldBeFalse();
    }

    [Fact]
    public void Calcular_Ciclo_DetectsCycle()
    {
        var a = CrearReceta(Guid.NewGuid());
        var b = CrearReceta(Guid.NewGuid());
        a.Insumos.Add(SubReceta(b.Id, 1m));
        b.Insumos.Add(SubReceta(a.Id, 1m));

        var recetas = new Dictionary<Guid, Receta> { [a.Id] = a, [b.Id] = b };
        var resultado = CostoService.Calcular(a, id => recetas.GetValueOrDefault(id), _ => 10m);

        resultado.CicloDetectado.ShouldBeTrue();
    }
}

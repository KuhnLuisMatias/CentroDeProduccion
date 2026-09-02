using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Domain.Services;

/// <summary>
/// Resolves the cost of a recipe by recursively expanding sub-recipes (BOM, spec §18.6).
/// Mirrors <see cref="ConversionUnidades"/> as a pure static service: it takes functional
/// lookups instead of referencing any persistence layer, so it stays testable without mocks.
/// </summary>
public static class CostoService
{
    /// <summary>Default maximum BOM depth before treating the recipe as cyclic.</summary>
    public const int MaxProfundidadDefault = 10;

    public sealed record CostoResult(
        decimal CostoInsumos,
        decimal CostoUnitario,
        bool CicloDetectado);

    /// <summary>
    /// Computes a recipe's cost. Cost is always the batch total of insumos at their last
    /// purchase price (no yield, no waste). <paramref name="obtenerReceta"/> resolves
    /// sub-recipes by id and <paramref name="obtenerPrecioInsumo"/> resolves a direct insumo's
    /// unit price.
    /// </summary>
    public static CostoResult Calcular(
        Receta receta,
        Func<Guid, Receta?> obtenerReceta,
        Func<Guid, decimal> obtenerPrecioInsumo,
        int maxProfundidad = MaxProfundidadDefault)
    {
        var (costoInsumos, ciclo) = ResolverReceta(
            receta, obtenerReceta, obtenerPrecioInsumo, new HashSet<Guid>(), 0, maxProfundidad);

        return new CostoResult(costoInsumos, costoInsumos, ciclo);
    }

    private static (decimal Costo, bool Ciclo) ResolverReceta(
        Receta receta,
        Func<Guid, Receta?> obtenerReceta,
        Func<Guid, decimal> obtenerPrecioInsumo,
        HashSet<Guid> enProceso,
        int profundidad,
        int maxProfundidad)
    {
        if (profundidad >= maxProfundidad || !enProceso.Add(receta.Id))
        {
            return (0, true);
        }

        decimal costo = 0;
        foreach (var detalle in receta.Insumos)
        {
            if (detalle.InsumoId.HasValue)
            {
                costo += obtenerPrecioInsumo(detalle.InsumoId.Value) * detalle.CantidadNecesaria;
            }
            else if (detalle.RecetaOrigenId.HasValue)
            {
                var subReceta = obtenerReceta(detalle.RecetaOrigenId.Value);
                if (subReceta is not null)
                {
                    var (subCosto, subCiclo) = ResolverReceta(
                        subReceta, obtenerReceta, obtenerPrecioInsumo, enProceso, profundidad + 1, maxProfundidad);

                    if (subCiclo)
                    {
                        enProceso.Remove(receta.Id);
                        return (0, true);
                    }

                    // A sub-recipe line's quantity counts whole batches of that sub-recipe.
                    costo += subCosto * detalle.CantidadNecesaria;
                }
            }
        }

        enProceso.Remove(receta.Id);
        return (costo, false);
    }

    /// <summary>
    /// Flattens a recipe's BOM into the total quantity of each direct insumo required to
    /// produce one batch of <paramref name="receta"/>, expressed in each insumo's CONSUMPTION
    /// unit. A sub-recipe line's quantity counts whole batches of that sub-recipe
    /// (<c>cantidad × factor</c>). Cycle/depth are guarded. Recipe lines written
    /// in the insumo's purchase unit are converted via <see cref="Insumo.FactorConversion"/>
    /// (1 UnidadCompra = FactorConversion UnidadConsumo); a line in any other unit is
    /// unrecoverable and throws.
    /// </summary>
    public static Dictionary<Guid, decimal> ExplosionarInsumos(
        Receta receta,
        Func<Guid, Receta?> obtenerReceta,
        Func<Guid, Insumo?> obtenerInsumo,
        int maxProfundidad = MaxProfundidadDefault)
    {
        var resultado = new Dictionary<Guid, decimal>();
        Explosionar(receta, 1m, obtenerReceta, obtenerInsumo, resultado, new HashSet<Guid>(), 0, maxProfundidad);
        return resultado;
    }

    private static void Explosionar(
        Receta receta,
        decimal factor,
        Func<Guid, Receta?> obtenerReceta,
        Func<Guid, Insumo?> obtenerInsumo,
        Dictionary<Guid, decimal> resultado,
        HashSet<Guid> enProceso,
        int profundidad,
        int maxProfundidad)
    {
        if (profundidad >= maxProfundidad || !enProceso.Add(receta.Id))
        {
            return;
        }

        foreach (var detalle in receta.Insumos)
        {
            if (detalle.InsumoId.HasValue)
            {
                var cantidad = detalle.CantidadNecesaria * factor;

                var insumo = obtenerInsumo(detalle.InsumoId.Value);
                if (insumo is not null && detalle.UnidadMedidaId != insumo.UnidadConsumoId)
                {
                    if (detalle.UnidadMedidaId == insumo.UnidadCompraId)
                    {
                        cantidad *= insumo.FactorConversion;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"La unidad de la línea de receta no coincide con las unidades del insumo {insumo.Nombre}");
                    }
                }

                resultado[detalle.InsumoId.Value] = resultado.GetValueOrDefault(detalle.InsumoId.Value) + cantidad;
            }
            else if (detalle.RecetaOrigenId.HasValue)
            {
                var subReceta = obtenerReceta(detalle.RecetaOrigenId.Value);
                if (subReceta is not null)
                {
                    var subFactor = factor * detalle.CantidadNecesaria;
                    Explosionar(subReceta, subFactor, obtenerReceta, obtenerInsumo, resultado, enProceso, profundidad + 1, maxProfundidad);
                }
            }
        }

        enProceso.Remove(receta.Id);
    }
}

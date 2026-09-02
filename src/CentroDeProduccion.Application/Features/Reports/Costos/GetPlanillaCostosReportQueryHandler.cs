using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Builds the cost worksheet for one recipe, replicating the per-product sheets of the original
/// PLANTILLA COSTOS.xlsx. Unlike the aggregate costing (<see cref="RecetaCostoResolver"/>), every
/// BOM line is priced individually: direct insumo lines use the last purchase price (falling back
/// to the weighted average) converted from purchase to consumption units when the line is written
/// in the insumo's consumption unit — mirroring <see cref="CostoService.ExplosionarInsumos"/>'s
/// line-level conversion rules; sub-recipe lines use the referenced recipe's standard unit cost.
/// </summary>
public class GetPlanillaCostosReportQueryHandler
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly RecetaCostoResolver _recetaCostoResolver;

    public GetPlanillaCostosReportQueryHandler(
        IRecetaRepository recetaRepository,
        IInsumoRepository insumoRepository,
        RecetaCostoResolver recetaCostoResolver)
    {
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
        _recetaCostoResolver = recetaCostoResolver;
    }

    public async Task<Result<GetPlanillaCostosReportDto>> HandleAsync(
        GetPlanillaCostosReportQuery query, CancellationToken ct = default)
    {
        var receta = await _recetaRepository.GetByIdWithDetallesAsync(query.RecetaId, ct);
        if (receta is null)
        {
            return Result.Failure<GetPlanillaCostosReportDto>(
                Error.NotFound("RECETA_NOT_FOUND", "Receta no encontrada"));
        }

        var insumoIds = receta.Insumos
            .Where(d => d.InsumoId.HasValue)
            .Select(d => d.InsumoId!.Value)
            .ToList();
        var insumos = (await _insumoRepository.GetByIdsAsync(insumoIds, ct))
            .ToDictionary(i => i.Id);

        var subRecetaCostos = new Dictionary<Guid, decimal>();
        var items = new List<PlanillaCostosItem>();
        decimal costoInsumosLote = 0m;

        foreach (var detalle in receta.Insumos)
        {
            decimal precioUnitario;
            string referencia;
            string tipoLinea;

            if (detalle.InsumoId.HasValue &&
                insumos.TryGetValue(detalle.InsumoId.Value, out var insumo))
            {
                var precioBase = insumo.PrecioUltimaCompra;

                // Prices are per purchase unit (1 UnidadCompra = FactorConversion UnidadConsumo).
                // A line written in the consumption unit must divide to keep the subtotal correct,
                // same conversion rule CostoService applies when exploding the BOM.
                precioUnitario = detalle.UnidadMedidaId == insumo.UnidadConsumoId && insumo.FactorConversion > 0
                    ? precioBase / insumo.FactorConversion
                    : precioBase;

                referencia = insumo.Nombre;
                tipoLinea = "Insumo";
            }
            else if (detalle.RecetaOrigenId is not null)
            {
                precioUnitario = await ResolverCostoUnitarioSubRecetaAsync(detalle.RecetaOrigenId.Value, subRecetaCostos, ct);
                referencia = detalle.RecetaOrigen?.Nombre ?? string.Empty;
                tipoLinea = "SubReceta";
            }
            else
            {
                continue;
            }

            var subtotal = detalle.CantidadNecesaria * precioUnitario;
            costoInsumosLote += subtotal;

            items.Add(new PlanillaCostosItem(
                referencia,
                tipoLinea,
                detalle.CantidadNecesaria,
                detalle.UnidadMedida.Simbolo,
                Math.Round(precioUnitario, 4),
                Math.Round(subtotal, 2),
                detalle.Observaciones));
        }

        var costoUnitario = costoInsumosLote;

        var header = new PlanillaCostosRecetaHeader(
            receta.Id,
            receta.Nombre,
            receta.Categoria.Nombre);

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            null,
            null,
            $"Receta: {receta.Nombre}",
            "planilla-costos",
            $"Planilla de costos - {receta.Nombre}");

        return Result.Success(new GetPlanillaCostosReportDto(
            header,
            items,
            new PlanillaCostosTotales(
                Math.Round(costoInsumosLote, 2),
                Math.Round(costoUnitario, 2)),
            metadata));
    }

    private async Task<decimal> ResolverCostoUnitarioSubRecetaAsync(
        Guid subRecetaId,
        Dictionary<Guid, decimal> cache,
        CancellationToken ct)
    {
        if (cache.TryGetValue(subRecetaId, out var costo))
        {
            return costo;
        }

        var subReceta = await _recetaRepository.GetByIdWithDetallesAsync(subRecetaId, ct);
        if (subReceta is null)
        {
            cache[subRecetaId] = 0m;
            return 0m;
        }

        var resultado = await _recetaCostoResolver.CalcularAsync(subReceta, ct);
        cache[subRecetaId] = resultado.CostoUnitario;
        return resultado.CostoUnitario;
    }
}

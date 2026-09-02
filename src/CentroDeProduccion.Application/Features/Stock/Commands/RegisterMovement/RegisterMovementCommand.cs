using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Stock.Commands.RegisterMovement;

/// <summary>
/// Registers a stock movement against EITHER an insumo (<see cref="InsumoId"/>) OR a finished
/// product (<see cref="ProductoTerminadoId"/>). Exactly one target must be set. For insumos,
/// <paramref name="Cantidad"/> is converted to the consumption unit via
/// <see cref="CentroDeProduccion.Domain.Services.ConversionUnidades"/>.
/// </summary>
public sealed record RegisterMovementCommand(
    Guid? InsumoId,
    Guid? ProductoTerminadoId,
    TipoMovimientoStock Tipo,
    decimal Cantidad,
    Guid UnidadOriginalId,
    decimal? PrecioUnitario,
    string Motivo,
    string? DocumentoOrigen);

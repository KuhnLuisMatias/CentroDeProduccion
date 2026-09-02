using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Query for the insumo stock-movements report, optionally filtered by date range and movement
/// type. Only movements targeting an insumo are included.
/// </summary>
public sealed record GetStockInsumosMovimientosReportQuery(
    DateTime? From = null,
    DateTime? To = null,
    TipoMovimientoStock? Tipo = null);

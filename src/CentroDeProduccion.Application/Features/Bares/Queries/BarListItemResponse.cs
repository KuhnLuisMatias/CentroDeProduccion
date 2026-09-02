using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Bares.Queries;

public sealed record BarListItemResponse(
    Guid Id,
    string Nombre,
    string Direccion,
    string? Encargado,
    EstadoBar Estado,
    decimal MargenReventaPorcentaje);
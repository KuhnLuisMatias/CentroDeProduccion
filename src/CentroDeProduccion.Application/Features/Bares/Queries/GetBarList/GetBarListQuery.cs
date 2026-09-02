using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Bares.Queries.GetBarList;

public sealed record GetBarListQuery(
    EstadoBar? Estado,
    string? SearchTerm);
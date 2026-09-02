using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Bares.Commands.CreateBar;

public sealed record CreateBarResponse(
    Guid Id,
    string Nombre,
    EstadoBar Estado,
    byte[] RowVersion);
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Inventario.Commands.CreateInventarioSesion;

public sealed record CreateInventarioSesionCommand(
    TipoInventario TipoInventario,
    Guid? ResponsableId,
    string? Notas);

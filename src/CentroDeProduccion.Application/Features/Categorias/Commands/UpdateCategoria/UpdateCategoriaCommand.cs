using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Categorias.Commands.UpdateCategoria;

public sealed record UpdateCategoriaCommand(
    Guid Id,
    string Nombre,
    AmbitoCategoria Ambito,
    bool Activo);

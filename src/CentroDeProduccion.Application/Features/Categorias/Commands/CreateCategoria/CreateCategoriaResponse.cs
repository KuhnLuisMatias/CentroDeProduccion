using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Categorias.Commands.CreateCategoria;

public sealed record CreateCategoriaResponse(
    Guid Id,
    string Nombre,
    AmbitoCategoria Ambito);

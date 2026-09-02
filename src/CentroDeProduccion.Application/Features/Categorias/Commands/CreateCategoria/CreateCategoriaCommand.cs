using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Categorias.Commands.CreateCategoria;

public sealed record CreateCategoriaCommand(
    string Nombre,
    AmbitoCategoria Ambito);

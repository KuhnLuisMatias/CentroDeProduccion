using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.UnidadesMedida.Commands.CreateUnidadMedida;

public sealed record CreateUnidadMedidaCommand(
    string Nombre,
    string Simbolo,
    TipoUnidadMedida Tipo);

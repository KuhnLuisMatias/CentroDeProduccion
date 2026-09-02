using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.UnidadesMedida.Commands.CreateUnidadMedida;

public sealed record CreateUnidadMedidaResponse(
    Guid Id,
    string Nombre,
    string Simbolo,
    TipoUnidadMedida Tipo);

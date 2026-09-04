using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.UnidadesMedida.Commands.UpdateUnidadMedida;

public sealed record UpdateUnidadMedidaCommand(
    Guid Id,
    string Nombre,
    string Simbolo,
    TipoUnidadMedida Tipo,
    bool Activo);

using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.GenerarOCDesdeAlertas;

public sealed record GenerarOCDesdeAlertasCommand(IReadOnlyList<Guid> InsumoIds);

public sealed record OrdenCompraGeneradaResponse(
    Guid Id,
    int Numero,
    Guid ProveedorId,
    string ProveedorNombre,
    int CantidadItems,
    EstadoOrdenCompra Estado);

public sealed record GenerarOCDesdeAlertasResponse(IReadOnlyList<OrdenCompraGeneradaResponse> Ordenes);
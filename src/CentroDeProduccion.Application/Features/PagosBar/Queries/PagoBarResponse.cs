using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.PagosBar.Queries;

public sealed record PagoBarMetodoResponse(MetodoPago Tipo, decimal Monto, string? Referencia);

public sealed record PagoBarItemResponse(Guid RemitoId, int RemitoNumeroRemito, decimal MontoAplicado);

public sealed record PagoBarResponse(
    Guid Id,
    int Numero,
    Guid BarId,
    string BarNombre,
    DateTime FechaPago,
    decimal MontoTotal,
    string? Observaciones,
    IReadOnlyList<PagoBarMetodoResponse> Metodos,
    IReadOnlyList<PagoBarItemResponse> Items);

public sealed record PagoBarListResponse(
    Guid Id,
    int Numero,
    Guid BarId,
    string BarNombre,
    DateTime FechaPago,
    decimal MontoTotal,
    int MetodoCount);
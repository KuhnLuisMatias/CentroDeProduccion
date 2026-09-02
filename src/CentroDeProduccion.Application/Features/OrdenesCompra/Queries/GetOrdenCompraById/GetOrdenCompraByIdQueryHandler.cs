using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.OrdenesCompra.Queries;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Queries.GetOrdenCompraById;

public class GetOrdenCompraByIdQueryHandler
{
    private readonly IOrdenCompraRepository _ordenCompraRepository;

    public GetOrdenCompraByIdQueryHandler(IOrdenCompraRepository ordenCompraRepository)
    {
        _ordenCompraRepository = ordenCompraRepository;
    }

    public async Task<Result<OrdenCompraResponse>> HandleAsync(GetOrdenCompraByIdQuery query, CancellationToken cancellationToken = default)
    {
        var ordenCompra = await _ordenCompraRepository.GetByIdWithItemsAsync(query.Id, cancellationToken);
        if (ordenCompra == null)
        {
            return Result.Failure<OrdenCompraResponse>(Error.NotFound("ORDEN_COMPRA_NOT_FOUND", "Orden de compra no encontrada"));
        }

        return Result.Success(Map(ordenCompra));
    }

    internal static OrdenCompraResponse Map(Domain.Entities.OrdenCompra ordenCompra) => new(
        ordenCompra.Id,
        ordenCompra.Numero,
        ordenCompra.ProveedorId,
        ordenCompra.Proveedor?.NombreRazonSocial ?? string.Empty,
        ordenCompra.Estado,
        ordenCompra.FechaCreacion,
        ordenCompra.FechaEnvio,
        ordenCompra.Observaciones,
        ordenCompra.Items.Sum(i => i.CantidadPedida * i.PrecioUnitario),
        ordenCompra.Items
            .Select(i => new OrdenCompraItemResponse(
                i.Id,
                i.InsumoId,
                i.Insumo?.Nombre ?? string.Empty,
                i.CantidadPedida,
                i.PrecioUnitario,
                i.CantidadPedida * i.PrecioUnitario))
            .ToList(),
        ordenCompra.RowVersion);
}
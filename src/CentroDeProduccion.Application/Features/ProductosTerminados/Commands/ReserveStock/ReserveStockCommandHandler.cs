using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.ProductosTerminados.Commands.ReserveStock;

/// <summary>
/// Reserves a quantity of a finished product, transitioning its state to Reservado when the
/// reserve consumes the available stock. Expired products cannot be reserved.
/// </summary>
public class ReserveStockCommandHandler
{
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReserveStockCommandHandler(
        IProductoTerminadoRepository productoTerminadoRepository,
        IUnitOfWork unitOfWork)
    {
        _productoTerminadoRepository = productoTerminadoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ReserveStockResponse>> HandleAsync(ReserveStockCommand command, CancellationToken cancellationToken = default)
    {
        var producto = await _productoTerminadoRepository.GetByIdAsync(command.ProductoTerminadoId, cancellationToken);
        if (producto == null)
        {
            return Result.Failure<ReserveStockResponse>(Error.NotFound("PRODUCTO_NOT_FOUND", "Producto terminado no encontrado"));
        }

        if (producto.Estado == EstadoProductoTerminado.Vencido || producto.FechaVencimiento < RelojDeNegocio.Ahora)
        {
            return Result.Failure<ReserveStockResponse>(
                Error.Validation("PRODUCTO_VENCIDO", "No se puede reservar un producto vencido"));
        }

        if (command.Cantidad > producto.StockActual)
        {
            return Result.Failure<ReserveStockResponse>(
                Error.Validation("INSUFFICIENT_STOCK", $"Stock insuficiente. Disponible: {producto.StockActual}"));
        }

        producto.Estado = EstadoProductoTerminado.Reservado;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReserveStockResponse(producto.Id, producto.Estado);
    }
}

using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CancelarOrdenCompra;

/// <summary>Cancels an order from Borrador or Enviada.</summary>
public class CancelarOrdenCompraCommandHandler
{
    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarOrdenCompraCommandHandler(
        IOrdenCompraRepository ordenCompraRepository,
        IUnitOfWork unitOfWork)
    {
        _ordenCompraRepository = ordenCompraRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CancelarOrdenCompraResponse>> HandleAsync(CancelarOrdenCompraCommand command, CancellationToken cancellationToken = default)
    {
        var ordenCompra = await _ordenCompraRepository.GetByIdWithItemsAsync(command.OrdenCompraId, cancellationToken);
        if (ordenCompra == null)
        {
            return Result.Failure<CancelarOrdenCompraResponse>(Error.NotFound("ORDEN_COMPRA_NOT_FOUND", "Orden de compra no encontrada"));
        }

        if (ordenCompra.Estado is not (EstadoOrdenCompra.Borrador or EstadoOrdenCompra.Enviada))
        {
            return Result.Failure<CancelarOrdenCompraResponse>(
                Error.Validation("ORDEN_NO_CANCELABLE", "No se puede cancelar la orden en su estado actual"));
        }

        ordenCompra.Estado = EstadoOrdenCompra.Cancelada;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<CancelarOrdenCompraResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La orden fue modificada por otro usuario. Reintente."));
        }

        return new CancelarOrdenCompraResponse(ordenCompra.Id, ordenCompra.Numero, ordenCompra.Estado);
    }
}
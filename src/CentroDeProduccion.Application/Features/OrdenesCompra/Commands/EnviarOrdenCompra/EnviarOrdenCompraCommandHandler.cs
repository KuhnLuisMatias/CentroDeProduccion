using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.EnviarOrdenCompra;

/// <summary>
/// Transitions an order from Borrador to Enviada, recording FechaEnvio. After this point the
/// header and items are immutable.
/// </summary>
public class EnviarOrdenCompraCommandHandler
{
    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EnviarOrdenCompraCommandHandler(
        IOrdenCompraRepository ordenCompraRepository,
        IUnitOfWork unitOfWork)
    {
        _ordenCompraRepository = ordenCompraRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EnviarOrdenCompraResponse>> HandleAsync(EnviarOrdenCompraCommand command, CancellationToken cancellationToken = default)
    {
        var ordenCompra = await _ordenCompraRepository.GetByIdAsync(command.OrdenCompraId, cancellationToken);
        if (ordenCompra == null)
        {
            return Result.Failure<EnviarOrdenCompraResponse>(Error.NotFound("ORDEN_COMPRA_NOT_FOUND", "Orden de compra no encontrada"));
        }

        if (ordenCompra.Estado != EstadoOrdenCompra.Borrador)
        {
            return Result.Failure<EnviarOrdenCompraResponse>(
                Error.Validation("ORDEN_NO_ENVIABLE", "Solo se pueden enviar órdenes en estado Borrador"));
        }

        ordenCompra.Estado = EstadoOrdenCompra.Enviada;
        ordenCompra.FechaEnvio = RelojDeNegocio.Ahora;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<EnviarOrdenCompraResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La orden fue modificada por otro usuario. Reintente."));
        }

        return new EnviarOrdenCompraResponse(ordenCompra.Id, ordenCompra.Numero, ordenCompra.Estado);
    }
}
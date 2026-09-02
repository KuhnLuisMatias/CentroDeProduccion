using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.UpdateOrdenCompra;

/// <summary>
/// Edits the header and replaces the items of a purchase order. Only orders in Borrador state
/// are editable; a RowVersion mismatch rejects the request with 409.
/// </summary>
public class UpdateOrdenCompraCommandHandler
{
    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateOrdenCompraCommand> _validator;

    public UpdateOrdenCompraCommandHandler(
        IOrdenCompraRepository ordenCompraRepository,
        IProveedorRepository proveedorRepository,
        IInsumoRepository insumoRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateOrdenCompraCommand> validator)
    {
        _ordenCompraRepository = ordenCompraRepository;
        _proveedorRepository = proveedorRepository;
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(UpdateOrdenCompraCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var ordenCompra = await _ordenCompraRepository.GetByIdWithItemsAsync(command.Id, cancellationToken);
        if (ordenCompra == null)
        {
            return Result.Failure(Error.NotFound("ORDEN_COMPRA_NOT_FOUND", "Orden de compra no encontrada"));
        }

        if (ordenCompra.Estado != EstadoOrdenCompra.Borrador)
        {
            return Result.Failure(
                Error.Validation("ORDEN_NO_EDITABLE", "Solo se pueden editar órdenes en estado Borrador"));
        }

        if (!ordenCompra.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La orden fue modificada por otro usuario. Recargue e intente nuevamente."));
        }

        var proveedor = await _proveedorRepository.GetByIdAsync(command.ProveedorId, cancellationToken);
        if (proveedor == null || !proveedor.Activo)
        {
            return Result.Failure(Error.NotFound("PROVEEDOR_NOT_FOUND", "Proveedor no encontrado o inactivo"));
        }

        var insumoIds = command.Items.Select(i => i.InsumoId).Distinct().ToList();
        var insumos = await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken);
        var insumoDict = insumos.ToDictionary(i => i.Id);
        foreach (var insumoId in insumoIds)
        {
            if (!insumoDict.TryGetValue(insumoId, out var insumo) || !insumo.Activo)
            {
                return Result.Failure(Error.NotFound("INSUMO_NOT_FOUND", $"Insumo {insumoId} no encontrado o inactivo"));
            }
        }

        ordenCompra.ProveedorId = proveedor.Id;
        ordenCompra.Observaciones = command.Observaciones;

        ordenCompra.Items.Clear();
        foreach (var item in command.Items)
        {
            ordenCompra.Items.Add(new OrdenCompraItem
            {
                Id = Guid.NewGuid(),
                InsumoId = item.InsumoId,
                CantidadPedida = item.CantidadPedida,
                PrecioUnitario = item.PrecioUnitario
            });
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La orden fue modificada por otro usuario. Recargue e intente nuevamente."));
        }

        return Result.Success();
    }
}
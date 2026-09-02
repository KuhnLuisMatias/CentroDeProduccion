using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CreateOrdenCompra;

/// <summary>
/// Creates a purchase order in Borrador state with a sequential unique Numero. Validates that
/// the proveedor and every insumo exist and are active before persisting.
/// </summary>
public class CreateOrdenCompraCommandHandler
{
    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateOrdenCompraCommand> _validator;

    public CreateOrdenCompraCommandHandler(
        IOrdenCompraRepository ordenCompraRepository,
        IProveedorRepository proveedorRepository,
        IInsumoRepository insumoRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateOrdenCompraCommand> validator)
    {
        _ordenCompraRepository = ordenCompraRepository;
        _proveedorRepository = proveedorRepository;
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<CreateOrdenCompraResponse>> HandleAsync(CreateOrdenCompraCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateOrdenCompraResponse>(errors.First());
        }

        var proveedor = await _proveedorRepository.GetByIdAsync(command.ProveedorId, cancellationToken);
        if (proveedor == null || !proveedor.Activo)
        {
            return Result.Failure<CreateOrdenCompraResponse>(
                Error.NotFound("PROVEEDOR_NOT_FOUND", "Proveedor no encontrado o inactivo"));
        }

        var insumoIds = command.Items.Select(i => i.InsumoId).Distinct().ToList();
        var insumos = await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken);
        var insumoDict = insumos.ToDictionary(i => i.Id);
        foreach (var insumoId in insumoIds)
        {
            if (!insumoDict.TryGetValue(insumoId, out var insumo) || !insumo.Activo)
            {
                return Result.Failure<CreateOrdenCompraResponse>(
                    Error.NotFound("INSUMO_NOT_FOUND", $"Insumo {insumoId} no encontrado o inactivo"));
            }
        }

        var numero = await _ordenCompraRepository.GetNextNumeroAsync(cancellationToken);

        var ordenCompra = new OrdenCompra
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            ProveedorId = proveedor.Id,
            Estado = EstadoOrdenCompra.Borrador,
            FechaCreacion = RelojDeNegocio.Ahora,
            Observaciones = command.Observaciones,
            CreadoPor = _currentUser.UsuarioId!.Value,
            Items = command.Items.Select(i => new OrdenCompraItem
            {
                Id = Guid.NewGuid(),
                InsumoId = i.InsumoId,
                CantidadPedida = i.CantidadPedida,
                PrecioUnitario = i.PrecioUnitario
            }).ToList()
        };

        await _ordenCompraRepository.AddAsync(ordenCompra, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<CreateOrdenCompraResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La orden fue modificada por otro usuario. Reintente."));
        }

        var total = ordenCompra.Items.Sum(i => i.CantidadPedida * i.PrecioUnitario);
        return new CreateOrdenCompraResponse(ordenCompra.Id, ordenCompra.Numero, proveedor.Id, ordenCompra.Estado, total);
    }
}
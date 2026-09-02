using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Inventario;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Inventario.Commands.CreateInventarioSesion;

/// <summary>
/// Opens a guided inventory session (toma de inventario) for either insumos or finished
/// products. The item list is derived from the catalog: every active insumo or finished
/// product becomes one <see cref="InventarioConteo"/> pre-filled with the system stock, so
/// each line starts as ConteoOk. The whole session and its conteos are committed atomically.
/// </summary>
public class CreateInventarioSesionCommandHandler
{
    private readonly IInventarioSesionRepository _inventarioSesionRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateInventarioSesionCommand> _validator;

    public CreateInventarioSesionCommandHandler(
        IInventarioSesionRepository inventarioSesionRepository,
        IInsumoRepository insumoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateInventarioSesionCommand> validator)
    {
        _inventarioSesionRepository = inventarioSesionRepository;
        _insumoRepository = insumoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<CreateInventarioSesionResponse>> HandleAsync(
        CreateInventarioSesionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateInventarioSesionResponse>(errors.First());
        }

        var responsableId = command.ResponsableId ?? _currentUser.UsuarioId;
        if (responsableId == null)
        {
            return Result.Failure<CreateInventarioSesionResponse>(
                Error.Validation("RESPONSABLE_REQUERIDO", "Debe indicar un responsable o estar autenticado"));
        }

        var session = new InventarioSesion
        {
            Id = Guid.NewGuid(),
            TipoInventario = command.TipoInventario,
            Estado = EstadoInventario.Abierta,
            ResponsableId = responsableId.Value,
            Notas = command.Notas,
            Fecha = RelojDeNegocio.Ahora,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        if (command.TipoInventario == TipoInventario.Insumo)
        {
            var insumos = await _insumoRepository.GetAllActiveAsync(cancellationToken);
            foreach (var insumo in insumos)
            {
                session.Conteos.Add(new InventarioConteo
                {
                    Id = Guid.NewGuid(),
                    InventarioSesionId = session.Id,
                    InsumoId = insumo.Id,
                    CantidadSistema = insumo.StockActual,
                    CantidadContada = insumo.StockActual
                });
            }
        }
        else
        {
            var productos = await _productoTerminadoRepository.GetAllActiveAsync(cancellationToken);
            foreach (var producto in productos)
            {
                session.Conteos.Add(new InventarioConteo
                {
                    Id = Guid.NewGuid(),
                    InventarioSesionId = session.Id,
                    ProductoTerminadoId = producto.Id,
                    CantidadSistema = producto.StockActual,
                    CantidadContada = producto.StockActual
                });
            }
        }

        await _inventarioSesionRepository.AddAsync(session, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<CreateInventarioSesionResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La sesión fue modificada por otro usuario. Reintente."));
        }

        return new CreateInventarioSesionResponse(
            session.Id, session.TipoInventario, session.Fecha, session.Estado, session.Conteos.Count);
    }
}

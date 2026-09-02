using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Inventario;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Inventario.Commands.RegistrarConteo;

/// <summary>
/// Records the counted quantity (and optional notes) for a single inventory line. The session
/// must not be closed. The response recomputes Diferencia and ConteoOk from the new count.
/// </summary>
public class RegistrarConteoCommandHandler
{
    private readonly IInventarioSesionRepository _inventarioSesionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegistrarConteoCommand> _validator;

    public RegistrarConteoCommandHandler(
        IInventarioSesionRepository inventarioSesionRepository,
        IUnitOfWork unitOfWork,
        IValidator<RegistrarConteoCommand> validator)
    {
        _inventarioSesionRepository = inventarioSesionRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<RegistrarConteoResponse>> HandleAsync(
        RegistrarConteoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<RegistrarConteoResponse>(errors.First());
        }

        var session = await _inventarioSesionRepository.GetByIdWithConteosAsync(command.InventarioSesionId, cancellationToken);
        if (session == null)
        {
            return Result.Failure<RegistrarConteoResponse>(
                Error.NotFound("SESION_NOT_FOUND", "Sesión de inventario no encontrada"));
        }

        if (session.Estado == EstadoInventario.Cerrada)
        {
            return Result.Failure<RegistrarConteoResponse>(
                Error.Validation("SESION_CERRADA", "No se pueden registrar conteos en una sesión cerrada"));
        }

        var conteo = session.Conteos.FirstOrDefault(c => c.Id == command.ConteoId);
        if (conteo == null)
        {
            return Result.Failure<RegistrarConteoResponse>(
                Error.NotFound("CONTEO_NOT_FOUND", "Conteo no encontrado en la sesión"));
        }

        conteo.CantidadContada = command.CantidadContada;
        conteo.Observaciones = command.Observaciones;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<RegistrarConteoResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La sesión fue modificada por otro usuario. Reintente."));
        }

        return new RegistrarConteoResponse(
            conteo.Id, conteo.CantidadSistema, conteo.CantidadContada, conteo.Diferencia, conteo.ConteoOk);
    }
}

using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Empleados.Commands.DeleteEmpleado;

/// <summary>
/// Soft-deletes an employee (Activo=false) instead of removing the row.
/// </summary>
public class DeleteEmpleadoCommandHandler
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEmpleadoCommandHandler(
        IEmpleadoRepository empleadoRepository,
        IUnitOfWork unitOfWork)
    {
        _empleadoRepository = empleadoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteEmpleadoCommand command, CancellationToken cancellationToken = default)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (empleado == null)
        {
            return Result.Failure(Error.NotFound("EMPLEADO_NOT_FOUND", "Empleado no encontrado"));
        }

        if (!empleado.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(Error.Concurrency("CONCURRENCY_CONFLICT", "El empleado fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        empleado.Activo = false;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
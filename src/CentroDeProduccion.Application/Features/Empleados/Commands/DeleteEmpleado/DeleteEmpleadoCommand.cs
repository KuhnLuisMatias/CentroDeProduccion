namespace CentroDeProduccion.Application.Features.Empleados.Commands.DeleteEmpleado;

public sealed record DeleteEmpleadoCommand(
    Guid Id,
    byte[] RowVersion);

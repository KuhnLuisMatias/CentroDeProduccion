namespace CentroDeProduccion.Application.Features.Empleados.Commands.CreateEmpleado;

public sealed record CreateEmpleadoResponse(
    Guid Id,
    string Nombre,
    string Apellido,
    string Dni,
    decimal TarifaPorHora);

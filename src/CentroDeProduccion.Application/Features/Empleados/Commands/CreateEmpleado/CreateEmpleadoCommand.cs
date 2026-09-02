using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Empleados.Commands.CreateEmpleado;

public sealed record CreateEmpleadoCommand(
    string Nombre,
    string Apellido,
    string Dni,
    CargoEmpleado Cargo,
    decimal TarifaPorHora,
    CategoriaEmpleado Categoria);

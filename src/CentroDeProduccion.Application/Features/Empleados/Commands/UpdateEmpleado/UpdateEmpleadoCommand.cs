using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Empleados.Commands.UpdateEmpleado;

public sealed record UpdateEmpleadoCommand(
    Guid Id,
    string Nombre,
    string Apellido,
    string Dni,
    CargoEmpleado Cargo,
    decimal TarifaPorHora,
    CategoriaEmpleado Categoria,
    bool Activo,
    byte[] RowVersion);

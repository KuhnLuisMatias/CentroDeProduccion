using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Nombre,
    string Apellido,
    string Email,
    string Password,
    Rol Rol,
    string? Telefono,
    string? Direccion);

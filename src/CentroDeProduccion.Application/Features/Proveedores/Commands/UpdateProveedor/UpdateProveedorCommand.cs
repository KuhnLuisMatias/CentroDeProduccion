namespace CentroDeProduccion.Application.Features.Proveedores.Commands.UpdateProveedor;

public sealed record UpdateProveedorCommand(
    Guid Id,
    string NombreRazonSocial,
    string Cuit,
    string Direccion,
    string? Telefono,
    string? WhatsApp,
    string? Email,
    string? PersonaContacto,
    string? HorarioAtencion,
    string CategoriasProvee,
    string TipoFactura,
    string? Observaciones,
    bool Activo);

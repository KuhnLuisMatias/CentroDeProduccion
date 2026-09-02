namespace CentroDeProduccion.Application.Features.Bares.Commands.UpdateBar;

public sealed record UpdateBarCommand(
    Guid Id,
    string Nombre,
    string Direccion,
    string? Encargado,
    string? Telefono,
    string? HorarioRecepcion,
    decimal MargenReventaPorcentaje,
    byte[] RowVersion);
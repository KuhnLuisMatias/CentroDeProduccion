namespace CentroDeProduccion.Application.Features.Bares.Commands.CreateBar;

public sealed record CreateBarCommand(
    string Nombre,
    string Direccion,
    string? Encargado,
    string? Telefono,
    string? HorarioRecepcion,
    decimal MargenReventaPorcentaje);
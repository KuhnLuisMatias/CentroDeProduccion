using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Bares.Queries;

public sealed record BarResponse(
    Guid Id,
    string Nombre,
    string Direccion,
    string? Encargado,
    string? Telefono,
    string? HorarioRecepcion,
    decimal MargenReventaPorcentaje,
    EstadoBar Estado,
    DateTime FechaCreacion,
    byte[] RowVersion);
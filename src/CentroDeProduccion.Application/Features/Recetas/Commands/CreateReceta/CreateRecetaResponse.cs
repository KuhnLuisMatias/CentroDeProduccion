namespace CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;

public sealed record CreateRecetaResponse(
    Guid Id,
    string Nombre,
    string CodigoSku);

namespace CentroDeProduccion.Application.Features.Recetas.Queries.CalcularCosto;

public sealed record CalcularCostoRecetaQuery(Guid RecetaId);

public sealed record CalcularCostoRecetaResponse(
    Guid RecetaId,
    string Nombre,
    decimal CostoInsumos,
    decimal CostoUnitario,
    bool CicloDetectado);

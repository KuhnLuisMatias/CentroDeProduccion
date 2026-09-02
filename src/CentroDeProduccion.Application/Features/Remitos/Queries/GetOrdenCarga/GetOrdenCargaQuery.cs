namespace CentroDeProduccion.Application.Features.Remitos.Queries.GetOrdenCarga;

public sealed record GetOrdenCargaQuery(Guid RemitoId, string Format = "a4");

public sealed record GetOrdenCargaQueryResponse(string Html);

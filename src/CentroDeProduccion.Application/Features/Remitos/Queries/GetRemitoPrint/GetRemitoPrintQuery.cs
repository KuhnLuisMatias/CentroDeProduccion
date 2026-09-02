namespace CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoPrint;

public sealed record GetRemitoPrintQuery(Guid Id, string Format = "a4");

public sealed record GetRemitoPrintQueryResponse(string Html);

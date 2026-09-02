using System.Net;
using CentroDeProduccion.Application.Abstractions;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Remitos.PrintModels;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoPrint;

/// <summary>
/// Renders a printable HTML view of a remito (header, line table, total and signature fields)
/// with an automatic print dialog on load. The template is selected from the requested format
/// (a4 or ticket) and rendered by <see cref="IPrintTemplateService"/>.
/// </summary>
public class GetRemitoPrintQueryHandler
{
    private readonly IRemitoRepository _remitoRepository;
    private readonly IPrintTemplateService _printTemplateService;

    public GetRemitoPrintQueryHandler(
        IRemitoRepository remitoRepository,
        IPrintTemplateService printTemplateService)
    {
        _remitoRepository = remitoRepository;
        _printTemplateService = printTemplateService;
    }

    public async Task<Result<GetRemitoPrintQueryResponse>> HandleAsync(GetRemitoPrintQuery query, CancellationToken cancellationToken = default)
    {
        var remito = await _remitoRepository.GetByIdWithLineasAsync(query.Id, cancellationToken);
        if (remito == null)
        {
            return Result.Failure<GetRemitoPrintQueryResponse>(Error.NotFound("REMITO_NOT_FOUND", "Remito no encontrado"));
        }

        var model = new PrintRemitoModel
        {
            NumeroRemito = remito.NumeroRemito,
            Fecha = remito.Fecha.ToString("dd/MM/yyyy HH:mm"),
            BarNombre = WebUtility.HtmlEncode(remito.Bar?.Nombre ?? string.Empty),
            BarDireccion = WebUtility.HtmlEncode(remito.Bar?.Direccion ?? string.Empty),
            LineasHtml = BuildLineasHtml(remito, ticket: query.Format == "ticket"),
            Total = remito.Lineas.Sum(l => l.Subtotal).ToString("N2"),
            EntregadoPor = WebUtility.HtmlEncode(remito.EntregadoPor ?? string.Empty),
            RecibidoPor = WebUtility.HtmlEncode(remito.RecibidoPor ?? string.Empty),
        };

        var html = _printTemplateService.Render(model, $"remito-{query.Format}");
        return Result.Success(new GetRemitoPrintQueryResponse(html));
    }

    private static string BuildLineasHtml(Domain.Entities.Remito remito, bool ticket)
    {
        var lineas = remito.Lineas;
        var priceColumns = !ticket;
        return string.Concat(lineas.Select(l =>
        {
            var descripcion = l.TipoLinea == TipoLineaRemito.ProductoTerminado
                ? l.ProductoTerminado?.Nombre ?? string.Empty
                : l.Insumo?.Nombre ?? string.Empty;

            var observaciones = l.Observaciones ?? string.Empty;
            if (ticket && observaciones.Length > 30)
            {
                observaciones = observaciones[..30];
            }

            var row = $"<tr>" +
                      $"<td>{WebUtility.HtmlEncode(descripcion)}</td>" +
                      (priceColumns ? $"<td class=\"num\">{l.Cantidad}</td>" +
                                     $"<td class=\"num\">{l.PrecioUnitario.ToString("N2")}</td>" : string.Empty) +
                      $"<td class=\"num\">{l.Subtotal.ToString("N2")}</td>" +
                      $"<td>{WebUtility.HtmlEncode(observaciones)}</td>" +
                      $"</tr>";
            return row;
        }));
    }
}

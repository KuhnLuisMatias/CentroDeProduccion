using System.Net;
using CentroDeProduccion.Application.Abstractions;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Remitos.PrintModels;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Queries.GetOrdenCarga;

/// <summary>
/// Renders a printable "orden de carga" HTML view for a remito: the lineas ordered by sequence
/// plus the bar contact data, with a blank chofer field. The template is selected from the
/// requested format (a4 or ticket) and rendered by <see cref="IPrintTemplateService"/>.
/// </summary>
public class GetOrdenCargaQueryHandler
{
    private readonly IRemitoRepository _remitoRepository;
    private readonly IPrintTemplateService _printTemplateService;

    public GetOrdenCargaQueryHandler(
        IRemitoRepository remitoRepository,
        IPrintTemplateService printTemplateService)
    {
        _remitoRepository = remitoRepository;
        _printTemplateService = printTemplateService;
    }

    public async Task<Result<GetOrdenCargaQueryResponse>> HandleAsync(GetOrdenCargaQuery query, CancellationToken cancellationToken = default)
    {
        var remito = await _remitoRepository.GetByIdWithLineasAsync(query.RemitoId, cancellationToken);
        if (remito == null)
        {
            return Result.Failure<GetOrdenCargaQueryResponse>(Error.NotFound("REMITO_NOT_FOUND", "Remito no encontrado"));
        }

        var lineas = remito.Lineas
            .OrderBy(l => l.Id)
            .Select((l, index) => (l, index));

        var lineasHtml = string.Concat(lineas.Select(x =>
        {
            var descripcion = x.l.TipoLinea == TipoLineaRemito.ProductoTerminado
                ? x.l.ProductoTerminado?.Nombre ?? string.Empty
                : x.l.Insumo?.Nombre ?? string.Empty;

            var observaciones = x.l.Observaciones ?? string.Empty;
            if (query.Format == "ticket" && observaciones.Length > 30)
            {
                observaciones = observaciones[..30];
            }

            return $"<tr>" +
                   $"<td>{x.index + 1}</td>" +
                   $"<td>{WebUtility.HtmlEncode(descripcion)}</td>" +
                   $"<td class=\"num\">{x.l.Cantidad}</td>" +
                   $"<td>{WebUtility.HtmlEncode(x.l.Lote ?? string.Empty)}</td>" +
                   $"<td>{WebUtility.HtmlEncode(observaciones)}</td>" +
                   $"</tr>";
        }));

        var model = new PrintRemitoModel
        {
            NumeroRemito = remito.NumeroRemito,
            BarNombre = WebUtility.HtmlEncode(remito.Bar?.Nombre ?? string.Empty),
            BarDireccion = WebUtility.HtmlEncode(remito.Bar?.Direccion ?? string.Empty),
            BarTelefono = WebUtility.HtmlEncode(remito.Bar?.Telefono ?? string.Empty),
            BarEncargado = WebUtility.HtmlEncode(remito.Bar?.Encargado ?? string.Empty),
            LineasHtml = lineasHtml,
            Chofer = string.Empty,
        };

        var html = _printTemplateService.Render(model, $"orden-carga-{query.Format}");
        return Result.Success(new GetOrdenCargaQueryResponse(html));
    }
}

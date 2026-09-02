using CentroDeProduccion.Application.Abstractions;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Remitos.PrintModels;
using CentroDeProduccion.Application.Features.Remitos.Queries.GetOrdenCarga;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the printable "orden de carga" view: the remito lines are rendered as table rows
/// and the bar contact data is wired into the print model, leaving the chofer field blank.
/// </summary>
public class GetOrdenCargaQueryHandlerTests
{
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IPrintTemplateService _printTemplateService = Substitute.For<IPrintTemplateService>();

    private GetOrdenCargaQueryHandler CreateHandler() => new(_remitoRepository, _printTemplateService);

    private static Remito CrearRemito(params RemitoLinea[] lineas) => new()
    {
        Id = Guid.NewGuid(),
        NumeroRemito = 14,
        BarId = Guid.NewGuid(),
        Bar = new Bar
        {
            Id = Guid.NewGuid(),
            Nombre = "Bar Centro",
            Direccion = "Av. Siempre Viva 123",
            Telefono = "555-1234",
            Encargado = "Juan Pérez"
        },
        Lineas = lineas
    };

    private static RemitoLinea LineaPT(string nombre, decimal cantidad) => new()
    {
        Id = Guid.NewGuid(),
        TipoLinea = TipoLineaRemito.ProductoTerminado,
        ProductoTerminado = new ProductoTerminado { Nombre = nombre },
        Cantidad = cantidad,
        Lote = "L-001"
    };

    [Fact]
    public async Task HandleAsync_RemitoValido_ComponeModeloConDatosDeBarLineasYChoferVacio()
    {
        var remito = CrearRemito(LineaPT("Pan Rústico", 5m), LineaPT("Facturas", 10m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id, Arg.Any<CancellationToken>()).Returns(remito);
        PrintRemitoModel? capturado = null;
        _printTemplateService.Render(Arg.Any<PrintRemitoModel>(), Arg.Any<string>())
            .Returns(ci =>
            {
                capturado = ci.Arg<PrintRemitoModel>();
                return "<html>rendered</html>";
            });

        var result = await CreateHandler().HandleAsync(new GetOrdenCargaQuery(remito.Id, "a4"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Html.ShouldBe("<html>rendered</html>");

        capturado.ShouldNotBeNull();
        capturado!.BarNombre.ShouldBe("Bar Centro");
        capturado.BarDireccion.ShouldBe("Av. Siempre Viva 123");
        capturado.BarTelefono.ShouldBe("555-1234");
        capturado.BarEncargado.ShouldBe("Juan P&#233;rez");
        capturado.Chofer.ShouldBe(string.Empty);
        capturado.LineasHtml.ShouldContain("Pan R&#250;stico");
        capturado.LineasHtml.ShouldContain("Facturas");
        capturado.LineasHtml.ShouldContain("L-001");

        _printTemplateService.Received(1).Render(
            Arg.Any<PrintRemitoModel>(),
            Arg.Is<string>(t => t == "orden-carga-a4"));
    }

    [Fact]
    public async Task HandleAsync_RemitoNoEncontrado_ReturnsRemitoNotFound()
    {
        _remitoRepository.GetByIdWithLineasAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Remito?)null);

        var result = await CreateHandler().HandleAsync(new GetOrdenCargaQuery(Guid.NewGuid(), "a4"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("REMITO_NOT_FOUND");
        _printTemplateService.DidNotReceive().Render(Arg.Any<PrintRemitoModel>(), Arg.Any<string>());
    }
}

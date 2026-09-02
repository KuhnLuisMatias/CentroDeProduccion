using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Infrastructure.Services;
using Shouldly;

namespace CentroDeProduccion.Tests.Infrastructure.Services;

/// <summary>
/// Verifies the report export services produce valid binary documents. Excel output must be a
/// ZIP container (0x50 0x4B magic) and PDF output must start with the "%PDF" header; both must
/// remain valid when the table has no data rows.
/// </summary>
public class ExportServiceTests
{
    private readonly ExcelExportService _excel = new();
    private readonly PdfExportService _pdf = new();

    private static ReportTable CrearTabla(int columnCount, int rowCount)
    {
        var columns = Enumerable.Range(1, columnCount)
            .Select(i => new ReportColumn($"col{i}", $"Columna {i}"))
            .ToList();

        var rows = Enumerable.Range(1, rowCount)
            .Select(r => new ReportRow(Enumerable.Range(1, columnCount)
                .Select(c => (object?)$"R{r}C{c}")
                .ToList()))
            .ToList();

        return new ReportTable(
            "Ventas",
            "Reporte de Ventas",
            new ReportMetadata(DateTime.Now),
            columns,
            rows);
    }

    [Fact]
    public async Task ExportToExcelAsync_TablaConFilas_DevuelveZipValido()
    {
        var bytes = await _excel.ExportToExcelAsync(CrearTabla(columnCount: 3, rowCount: 3));

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        bytes[0].ShouldBe((byte)0x50); // 'P'
        bytes[1].ShouldBe((byte)0x4B); // 'K'
    }

    [Fact]
    public async Task ExportToExcelAsync_SinFilas_DevuelveZipValidoSoloConCabecera()
    {
        var bytes = await _excel.ExportToExcelAsync(CrearTabla(columnCount: 3, rowCount: 0));

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        bytes[0].ShouldBe((byte)0x50);
        bytes[1].ShouldBe((byte)0x4B);
    }

    [Fact]
    public async Task ExportToExcelAsync_ReportTypeInvalido_SanitizaNombreDeHoja()
    {
        var table = CrearTabla(columnCount: 2, rowCount: 1) with { ReportType = "Ventas/Mensual" };

        var bytes = await _excel.ExportToExcelAsync(table);

        bytes.Length.ShouldBeGreaterThan(0);
        bytes[0].ShouldBe((byte)0x50);
        bytes[1].ShouldBe((byte)0x4B);
    }

    [Fact]
    public async Task ExportToPdfAsync_TablaConFilas_DevuelvePdfValido()
    {
        var bytes = await _pdf.ExportToPdfAsync(CrearTabla(columnCount: 3, rowCount: 3));

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
        bytes[0].ShouldBe((byte)0x25); // '%'
        bytes[1].ShouldBe((byte)0x50); // 'P'
        bytes[2].ShouldBe((byte)0x44); // 'D'
        bytes[3].ShouldBe((byte)0x46); // 'F'
    }

    [Fact]
    public async Task ExportToPdfAsync_SinFilas_DevuelvePdfValido()
    {
        var bytes = await _pdf.ExportToPdfAsync(CrearTabla(columnCount: 3, rowCount: 0));

        bytes.Length.ShouldBeGreaterThan(0);
        bytes[0].ShouldBe((byte)0x25);
        bytes[1].ShouldBe((byte)0x50);
        bytes[2].ShouldBe((byte)0x44);
        bytes[3].ShouldBe((byte)0x46);
    }

    [Fact]
    public async Task ExportToPdfAsync_MasDeCincoColumnas_DevuelvePdfValidoEnPaisaje()
    {
        var bytes = await _pdf.ExportToPdfAsync(CrearTabla(columnCount: 6, rowCount: 2));

        bytes.Length.ShouldBeGreaterThan(0);
        bytes[0].ShouldBe((byte)0x25);
        bytes[1].ShouldBe((byte)0x50);
        bytes[2].ShouldBe((byte)0x44);
        bytes[3].ShouldBe((byte)0x46);
    }
}

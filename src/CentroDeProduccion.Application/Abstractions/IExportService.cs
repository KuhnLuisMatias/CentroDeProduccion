using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Abstractions;

/// <summary>
/// Renders a <see cref="ReportTable"/> into a binary document (Excel or PDF).
/// Implementations are format-specific and registered as separate services so callers can
/// inject the concrete exporter they need.
/// </summary>
public interface IExportService
{
    Task<byte[]> ExportToExcelAsync(ReportTable report, CancellationToken ct = default);
    Task<byte[]> ExportToPdfAsync(ReportTable report, CancellationToken ct = default);
}

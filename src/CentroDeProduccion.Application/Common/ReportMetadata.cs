namespace CentroDeProduccion.Application.Common;

/// <summary>
/// Provenance and context for a generated report: when it was produced, the date range it
/// covers (when applicable), a human-readable description of any applied filters, and the
/// report kind/title used for headings and file naming.
/// </summary>
public sealed record ReportMetadata(
    DateTime GeneratedAt,
    DateTime? DateRangeFrom = null,
    DateTime? DateRangeTo = null,
    string? FilterDescription = null,
    string? ReportType = null,
    string? ReportTitle = null);

namespace CentroDeProduccion.Application.Common;

/// <summary>
/// A tabular report ready to be exported: <paramref name="ReportType"/> identifies the kind of
/// report (used as the worksheet/file name), <paramref name="ReportTitle"/> is the human-readable
/// heading, <paramref name="Metadata"/> carries generation context, and <paramref name="Columns"/>/
/// <paramref name="Rows"/> hold the actual data.
/// </summary>
public sealed record ReportTable(
    string ReportType,
    string ReportTitle,
    ReportMetadata Metadata,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<ReportRow> Rows);

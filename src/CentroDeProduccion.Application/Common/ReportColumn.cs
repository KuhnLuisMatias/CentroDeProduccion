namespace CentroDeProduccion.Application.Common;

/// <summary>
/// Describes a single column of a report table: the machine-readable <paramref name="Name"/>,
/// the <paramref name="Header"/> shown to the user, and an optional <paramref name="Format"/>
/// (e.g. a numeric/date format string) used when rendering cell values.
/// </summary>
public sealed record ReportColumn(string Name, string Header, string? Format = null);

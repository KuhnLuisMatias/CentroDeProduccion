namespace CentroDeProduccion.Application.Common;

/// <summary>
/// A single data row of a report table. <see cref="Cells"/> holds one value per column, in
/// column order; values are boxed (object?) because cells may hold strings, numbers, dates, etc.
/// </summary>
public sealed record ReportRow(IReadOnlyList<object?> Cells)
{
    public static ReportRow Empty { get; } = new(Array.Empty<object?>());
}

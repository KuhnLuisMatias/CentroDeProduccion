namespace CentroDeProduccion.Application.Features.Dashboard.Queries;

/// <summary>A single chart dataset (one series) within a chart.</summary>
public sealed record ChartDataset(string Label, IReadOnlyList<decimal> Data, string? BackgroundColor = null);

/// <summary>A chart with a type hint, title, axis labels and one or more datasets.</summary>
public sealed record ChartDto(
    string Type,
    string Title,
    IReadOnlyList<string> Labels,
    IReadOnlyList<ChartDataset> Datasets);

/// <summary>The set of charts shown on the dashboard.</summary>
public sealed record DashboardChartsDto(IReadOnlyList<ChartDto> Charts);

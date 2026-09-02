namespace CentroDeProduccion.Application.Common;

/// <summary>
/// A page of <typeparamref name="TItem"/> plus the metadata needed to render pagination
/// controls. Returned as the success value inside a <see cref="Result{TValue}"/> from
/// list/search handlers (e.g. <c>GET /api/insumos</c>).
/// </summary>
public sealed class PagedResult<TItem>
{
    public IReadOnlyList<TItem> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PagedResult(IReadOnlyList<TItem> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}

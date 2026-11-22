namespace HoneyHouse.Admin.Models;

public sealed record Pager(int Page, int TotalPages, int TotalCount)
{
    public static Pager From<T>(HoneyHouse.Application.Common.PagedResult<T> r) => new(r.Page, r.TotalPages, r.TotalCount);
}

namespace OT.Assessment.App.Models
{
    // using a Template as in a real word scenario we would likely have several endpoints that return paged results
    public record PagedResult<T>(
        IEnumerable<T> Data,
        int Page,
        int PageSize,
        long Total,
        int TotalPages
    );
}

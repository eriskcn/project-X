using Microsoft.EntityFrameworkCore;

namespace ProjectX.Data;

public static class DbSetExtensions
{
    public static IQueryable<T> IgnoreSoftDelete<T>(this IQueryable<T> query) where T : class, ISoftDelete
    {
        return query.IgnoreQueryFilters();
    }
}

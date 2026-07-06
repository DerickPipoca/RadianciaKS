namespace RadianciaKS.Application.Extensions
{
    public static class QueryableExtensions
    {
        public static IOrderedQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? sortBy, bool isDescending, Func<string?, bool, IOrderedQueryable<T>> sortLogic)
        {
            return sortLogic(sortBy?.ToLower(), isDescending);
        }
    }
}
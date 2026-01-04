using System.Linq.Dynamic.Core;

namespace DevHabit.Api.Services.Sorting;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sort,
        SortMapping[] sortMappings,
        string defaultOrderBy = "Id")
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return query.OrderBy(defaultOrderBy);
        }

        string[] sortFields = sort
            .Split(',')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        var orderByParts = new List<string>();

        foreach (string field in sortFields)
        {
            (string sortField, bool isDescending) = ParseSortField(field);

            SortMapping sortMapping =
                sortMappings.First(x => x.SortField.Equals(sortField, StringComparison.OrdinalIgnoreCase));

            string direction = (isDescending, sortMapping.Reverse) switch
            {
                (false, false) => "ASC",
                (false, true) => "DESC",
                (true, false) => "DESC",
                (true, true) => "ASC"
            };

            orderByParts.Add($"{sortMapping.PropertyName} {direction}");
        }

        string orderBy = string.Join(",", orderByParts);

        return query.OrderBy(orderBy);
    }

    private static (string SortField, bool IsDescending) ParseSortField(string field)
    {
        string[] parts = field.Split(' ');
        string sortField = parts[0];
        bool isDescending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortField, isDescending);
    }
}

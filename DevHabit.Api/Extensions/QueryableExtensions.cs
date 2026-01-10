using System.Linq.Dynamic.Core;
using DevHabit.Api.Services.Sorting;

namespace DevHabit.Api.Extensions;

public static class QueryableExtensions
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> ApplySort(string? sort,
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
                string[] parts = field.Split(' ');
                string sortField = parts[0];
                bool isDescending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

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
    }
}

namespace DevHabit.Api.Services.Sorting;

public sealed class SortMappingProvider(IEnumerable<ISortMappingDefinition> sortMappingDefinitions)
{
    public SortMapping[] GetMappings<TSource, TDestination>()
    {
        SortMappingDefinition<TSource, TDestination>? sortMappingDefinition =
            sortMappingDefinitions.OfType<SortMappingDefinition<TSource, TDestination>>().FirstOrDefault();

        if (sortMappingDefinition is null)
        {
            throw new InvalidOperationException(
                $"The mapping from '{typeof(TSource).Name}' to '{typeof(TDestination).Name}' is not defined");
        }

        return sortMappingDefinition.Mapping;
    }

    public bool ValidateMappings<TSource, TDestination>(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return true;
        }

        var sortFields = sort
            .Split(',')
            .Select(x => x.Trim().Split(' ')[0])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        SortMapping[] sortMappings = GetMappings<TSource, TDestination>();

        return sortFields.All(x => sortMappings.Any(y => y.SortField.Equals(x, StringComparison.OrdinalIgnoreCase)));
    }
}

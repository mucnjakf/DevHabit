namespace DevHabit.Api.Dtos.Tags;

public sealed class TagsQueryParameters
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

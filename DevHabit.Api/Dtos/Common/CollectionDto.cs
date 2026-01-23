namespace DevHabit.Api.Dtos.Common;

public sealed class CollectionDto<T> : ICollectionResponseDto<T>, ILinksResponseDto
{
    public List<T> Items { get; init; } = [];

    public List<LinkDto>? Links { get; set; }
}

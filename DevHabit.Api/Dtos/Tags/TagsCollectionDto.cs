using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Dtos.Tags;

public sealed record TagsCollectionDto : ICollectionResponse<TagDto>
{
    public List<TagDto> Items { get; init; }
}

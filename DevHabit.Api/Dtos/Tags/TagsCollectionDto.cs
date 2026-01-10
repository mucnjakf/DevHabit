using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Dtos.Tags;

public sealed record TagsCollectionDto : ICollectionResponseDto<TagDto>
{
    public required List<TagDto> Items { get; init; }

    public List<LinkDto> Links { get; set; }
}

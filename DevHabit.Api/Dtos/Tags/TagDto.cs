using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Dtos.Tags;

public sealed record TagDto : ILinksResponseDto
{
    public required string Id { get; init; }

    public required string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public List<LinkDto> Links { get; set; }
}

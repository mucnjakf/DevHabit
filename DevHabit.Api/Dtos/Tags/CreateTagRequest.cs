namespace DevHabit.Api.Dtos.Tags;

public sealed record CreateTagRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}

namespace DevHabit.Api.Dtos.Tags;

public sealed record UpdateTagRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }
}

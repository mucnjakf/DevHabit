namespace DevHabit.Api.Dtos.HabitTags;

public sealed record UpsertHabitTagsRequest
{
    public required List<string> TagIds { get; init; }
}

namespace DevHabit.Api.Dtos.HabitTags;

public sealed record UpsertHabitTagsDto
{
    public required List<string> TagIds { get; init; }
}

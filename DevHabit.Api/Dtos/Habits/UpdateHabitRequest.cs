using DevHabit.Api.Enums;

namespace DevHabit.Api.Dtos.Habits;

public sealed record UpdateHabitRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required HabitType Type { get; init; }

    public required FrequencyDto Frequency { get; init; }

    public required TargetDto Target { get; init; }

    public DateOnly? EndDate { get; init; }

    public UpdateMilestoneDto? Milestone { get; init; }
}

namespace DevHabit.Api.Dtos.Habits;

public sealed record TargetDto
{
    public required int Value { get; init; }

    public required string Unit { get; init; }
}

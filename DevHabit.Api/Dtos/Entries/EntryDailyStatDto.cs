namespace DevHabit.Api.Dtos.Entries;

public sealed record EntryDailyStatDto
{
    public DateOnly Date { get; init; }

    public int Count { get; init; }
}

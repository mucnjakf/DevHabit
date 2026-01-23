using DevHabit.Api.Enums;

namespace DevHabit.Api.Dtos.Entries;

public sealed record EntriesCursorQueryParameters
{
    public string? HabitId { get; init; }

    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public string? Cursor { get; init; }

    public string? Fields { get; init; }

    public EntrySource? Source { get; init; }

    public bool? IsArchived { get; init; }

    public int Limit { get; init; } = 10;
}

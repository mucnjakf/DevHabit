using DevHabit.Api.Enums;

namespace DevHabit.Api.Dtos.Entries;

public sealed record EntriesQueryParameters
{
    public string? HabitId { get; init; }

    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public string? Sort { get; init; }

    public string? Fields { get; init; }

    public EntrySource? Source { get; init; }

    public bool? IsArchived { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

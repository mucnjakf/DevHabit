using DevHabit.Api.Enums;

namespace DevHabit.Api.Entities;

public sealed class Entry
{
    public string Id { get; set; } = null!;

    public string HabitId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public int Value { get; set; }

    public string? Notes { get; set; }

    public EntrySource Source { get; init; }

    public string? ExternalId { get; init; }

    public bool IsArchived { get; set; }

    public DateOnly Date { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public Habit Habit { get; set; } = null!;
}

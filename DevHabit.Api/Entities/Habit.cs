using DevHabit.Api.Enums;

namespace DevHabit.Api.Entities;

public sealed class Habit
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public HabitType Type { get; set; }

    public Frequency Frequency { get; set; } = null!;

    public Target Target { get; set; } = null!;

    public HabitStatus Status { get; set; }

    public bool IsArchived { get; set; }

    public DateOnly? EndDate { get; set; }

    public Milestone? Milestone { get; set; }

    public DateTime? LastCompletedAtUtc { get; set; }

    public AutomationSource? AutomationSource { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string UserId { get; set; } = null!;

    public List<HabitTag> HabitTags { get; set; } = null!;

    public List<Tag> Tags { get; set; } = null!;
}

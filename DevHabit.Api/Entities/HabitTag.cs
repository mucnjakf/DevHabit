namespace DevHabit.Api.Entities;

public sealed class HabitTag
{
    public string HabitId { get; set; } = null!;

    public string TagId { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
}

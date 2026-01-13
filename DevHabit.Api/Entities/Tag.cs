namespace DevHabit.Api.Entities;

public sealed class Tag
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string UserId { get; set; } = null!;
}

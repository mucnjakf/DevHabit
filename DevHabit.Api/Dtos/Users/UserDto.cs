namespace DevHabit.Api.Dtos.Users;

public sealed record UserDto
{
    public required string Id { get; init; }

    public required string Email { get; init; }

    public required string Name { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required DateTime? UpdatedAtUtc { get; init; }
}

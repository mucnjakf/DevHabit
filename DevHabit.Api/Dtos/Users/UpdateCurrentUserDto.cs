namespace DevHabit.Api.Dtos.Users;

public sealed record UpdateCurrentUserDto
{
    public required string Name { get; init; }
}

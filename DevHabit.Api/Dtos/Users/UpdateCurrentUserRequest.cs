namespace DevHabit.Api.Dtos.Users;

public sealed record UpdateCurrentUserRequest
{
    public required string Name { get; init; }
}

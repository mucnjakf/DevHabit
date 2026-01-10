namespace DevHabit.Api.Dtos.Auth;

public sealed record RegisterUserRequest
{
    public required string Email { get; init; }

    public required string Name { get; init; }

    public required string Password { get; init; }

    public required string ConfirmPassword { get; init; }
}

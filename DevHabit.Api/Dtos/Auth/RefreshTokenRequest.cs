namespace DevHabit.Api.Dtos.Auth;

public sealed record RefreshTokenRequest
{
    public required string Value { get; init; }
}

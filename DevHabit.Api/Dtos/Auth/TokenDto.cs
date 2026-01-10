namespace DevHabit.Api.Dtos.Auth;

public sealed record TokenDto
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }
}

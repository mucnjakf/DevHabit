namespace DevHabit.Api.Options;

public sealed class JwtAuthOptions
{
    public const string Section = "JwtAuth";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string Key { get; init; }

    public required int ExpirationInMinutes { get; init; }

    public required int RefreshTokenExpirationInDays { get; init; }
}

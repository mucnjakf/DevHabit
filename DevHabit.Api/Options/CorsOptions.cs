namespace DevHabit.Api.Options;

public sealed class CorsOptions
{
    public const string Section = "Cors";

    public const string PolicyName = "DevHabitCorsPolicy";

    public required string[] AllowedOrigins { get; init; }
}

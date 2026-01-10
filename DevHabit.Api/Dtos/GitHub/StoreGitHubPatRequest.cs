namespace DevHabit.Api.Dtos.GitHub;

public sealed record StoreGitHubPatRequest
{
    public required string Pat { get; init; }

    public required int ExpiresInDays { get; init; }
}

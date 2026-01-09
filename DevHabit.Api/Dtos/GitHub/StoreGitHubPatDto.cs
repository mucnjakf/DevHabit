namespace DevHabit.Api.Dtos.GitHub;

public sealed record StoreGitHubPatDto
{
    public required string Pat { get; init; }

    public required int ExpiresInDays { get; init; }
}

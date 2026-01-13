namespace DevHabit.Api.Dtos.GitHub;

public sealed record GitHubEventRepoDto(
    long Id,
    string Name,
    Uri Url);

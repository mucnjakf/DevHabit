namespace DevHabit.Api.Dtos.GitHub;

public sealed record GitHubEventPayloadDto(
    string Action,
    ICollection<GitHubEventPayloadCommit>? Commits);

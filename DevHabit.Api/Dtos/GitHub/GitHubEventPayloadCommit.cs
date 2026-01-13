namespace DevHabit.Api.Dtos.GitHub;

public record GitHubEventPayloadCommit(
    string Sha,
    GitHubEventPayloadCommitAuthor Author,
    string Message,
    bool Distinct,
    Uri Url);

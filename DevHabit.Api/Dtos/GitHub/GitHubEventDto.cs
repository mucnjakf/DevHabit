namespace DevHabit.Api.Dtos.GitHub;

public sealed record GitHubEventDto(
    string Id,
    string Type,
    GitHubEventActorDto Actor,
    GitHubEventRepoDto Repo,
    GitHubEventPayloadDto Payload,
    bool Public,
    DateTimeOffset CreatedAt);

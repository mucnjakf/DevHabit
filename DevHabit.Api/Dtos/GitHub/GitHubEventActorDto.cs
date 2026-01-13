namespace DevHabit.Api.Dtos.GitHub;

public sealed record GitHubEventActorDto(
    long Id,
    string Login,
    string DisplayLogin,
    string GravatarId,
    Uri Url,
    Uri AvatarUrl);

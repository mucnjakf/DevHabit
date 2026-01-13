using DevHabit.Api.Dtos.Common;
using Newtonsoft.Json;

namespace DevHabit.Api.Dtos.GitHub;

public sealed record GitHubUserProfileDto : ILinksResponseDto
{
    public string Login { get; init; } = null!;

    public string Name { get; init; } = null!;

    [JsonProperty("avatar_url")]
    public string AvatarUrl { get; init; } = null!;

    public string Bio { get; init; } = null!;

    [JsonProperty("public_repos")]
    public int PublicRepos { get; init; }

    public int Followers { get; init; }

    public int Following { get; init; }

    public List<LinkDto>? Links { get; set; }
}

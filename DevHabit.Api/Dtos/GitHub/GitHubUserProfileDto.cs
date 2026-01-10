using DevHabit.Api.Dtos.Common;
using Newtonsoft.Json;

namespace DevHabit.Api.Dtos.GitHub;

public sealed record GitHubUserProfileDto : ILinksResponseDto
{
    public string Login { get; init; }

    public string Name { get; init; }

    [JsonProperty("avatar_url")]
    public string AvatarUrl { get; init; }

    public string Bio { get; init; }

    [JsonProperty("public_repos")]
    public int PublicRepos { get; init; }

    public int Followers { get; init; }

    public int Following { get; init; }

    public List<LinkDto> Links { get; set; }
}

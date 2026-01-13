namespace DevHabit.Api.Entities;

public sealed class GitHubPat
{
    public string Id { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string UserId { get; set; } = null!;
}

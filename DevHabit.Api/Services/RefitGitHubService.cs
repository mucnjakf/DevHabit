using System.Net.Http.Headers;
using DevHabit.Api.Dtos.GitHub;
using Newtonsoft.Json;
using Refit;

namespace DevHabit.Api.Services;

public sealed class RefitGitHubService(IGitHubApi gitHubApi, ILogger<GitHubService> logger)
{
    public async Task<GitHubUserProfileDto?> GetUserProfileAsync(
        string gitHubPat,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(gitHubPat);

        ApiResponse<GitHubUserProfileDto?> response = await gitHubApi.GetUserProfile(gitHubPat, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user profile - status code: {StatusCode}", response.StatusCode);
            return null;
        }

        return response.Content;
    }

    public async Task<IReadOnlyList<GitHubEventDto>?> GetUserEventsAsync(
        string username,
        string gitHubPat,
        int page = 1,
        int perPage = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(gitHubPat);
        ArgumentException.ThrowIfNullOrEmpty(username);

        ApiResponse<IReadOnlyList<GitHubEventDto>?> response = await gitHubApi
            .GetUserEvents(username, gitHubPat, page, perPage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user events - status code: {StatusCode}", response.StatusCode);
            return null;
        }

        return response.Content;
    }
}

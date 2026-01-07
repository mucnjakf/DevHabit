using System.Net.Http.Headers;
using DevHabit.Api.Dtos.GitHub;
using Newtonsoft.Json;

namespace DevHabit.Api.Services;

public sealed class GitHubService(IHttpClientFactory httpClientFactory, ILogger<GitHubService> logger)
{
    public async Task<GitHubUserProfileDto?> GetUserProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using HttpClient httpClient = CreateGitHubClient(accessToken);

        HttpResponseMessage httpResponseMessage = await httpClient.GetAsync("user", cancellationToken);

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Failed to get GitHub user profile - status code: {StatusCode}",
                httpResponseMessage.StatusCode);

            return null;
        }

        string content = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

        return JsonConvert.DeserializeObject<GitHubUserProfileDto>(content);
    }

    public async Task<IReadOnlyList<GitHubUserEventDto>?> GetUserEventsAsync(
        string username,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using HttpClient httpClient = CreateGitHubClient(accessToken);

        HttpResponseMessage httpResponseMessage = await httpClient
            .GetAsync($"users/{username}/events?per_page=100", cancellationToken);

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Failed to get GitHub user events - status code: {StatusCode}",
                httpResponseMessage.StatusCode);

            return null;
        }

        string content = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

        return JsonConvert.DeserializeObject<List<GitHubUserEventDto>>(content);
    }

    private HttpClient CreateGitHubClient(string accessToken)
    {
        HttpClient httpClient = httpClientFactory.CreateClient("github");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return httpClient;
    }
}

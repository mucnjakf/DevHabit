using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.IntegrationTests.Infrastructure;
using Newtonsoft.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace DevHabit.IntegrationTests.Tests;

public sealed class GitHubTests(DevHabitWebAppFactory factory) : IntegrationTestFixture(factory)
{
    private const string TestAccessToken = "gho_test123456789";

    private static readonly GitHubUserProfileDto User = new()
    {
        Login = "testuser",
        Name = "Test User",
        AvatarUrl = "https://github.com/testuser.png",
        Bio = "Test bio",
        PublicRepos = 10,
        Followers = 20,
        Following = 30
    };

    [Fact]
    public async Task GetProfile_ShouldReturnUserProfile_WhenAccessTokenIsValid()
    {
        WireMockServer
            .Given(Request.Create()
                .WithPath("/user")
                .WithHeader("Authorization", $"Bearer {TestAccessToken}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", MediaTypeNames.Application.Json)
                .WithBodyAsJson(User));

        HttpClient client = await CreateAuthenticatedClientAsync();

        var request = new StoreGitHubPatRequest
        {
            Pat = TestAccessToken,
            ExpiresInDays = 30
        };

        await client.PutAsJsonAsync(Routes.GitHub.StoreAccessToken, request);

        HttpResponseMessage response = await client.GetAsync(Routes.GitHub.GetProfile);
        response.EnsureSuccessStatusCode();

        string userProfileJson = await response.Content.ReadAsStringAsync();
        GitHubUserProfileDto? userProfile = JsonConvert.DeserializeObject<GitHubUserProfileDto>(userProfileJson);

        Assert.NotNull(userProfile);
        Assert.Equivalent(User, userProfile);
    }
}

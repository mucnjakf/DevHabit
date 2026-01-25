using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Auth;
using DevHabit.IntegrationTests.Infrastructure;

namespace DevHabit.IntegrationTests.Tests;

public sealed class AuthenticationTests(DevHabitWebAppFactory factory) : IntegrationTestFixture(factory)
{
    [Fact]
    public async Task Register_ShouldSucceed_WithValidParameters()
    {
        await CleanupDatabaseAsync();

        var request = new RegisterUserRequest
        {
            Email = "register@test.com",
            Name = "register@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(Routes.Auth.Register, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturnAccessTokens_WithValidParameters()
    {
        await CleanupDatabaseAsync();

        var request = new RegisterUserRequest
        {
            Email = "register1@test.com",
            Name = "register1@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(Routes.Auth.Register, request);
        response.EnsureSuccessStatusCode();

        TokenDto? token = await response.Content.ReadFromJsonAsync<TokenDto>();

        Assert.NotNull(token);
    }
}

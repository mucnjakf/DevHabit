using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Dtos.HabitTags;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Enums;
using DevHabit.FunctionalTests.Infrastructure;

namespace DevHabit.FunctionalTests.Tests;

public sealed class HabitManagementTests(DevHabitWebAppFactory factory) : FunctionalTestFixture(factory)
{
    [Fact]
    public async Task CompleteHabitManagementFlow_ShouldSucceed()
    {
        await CleanupDatabaseAsync();

        const string email = "habitflow@test.com";
        const string password = "Test123!";

        HttpClient httpClient = CreateClient();

        var registerUserRequest = new RegisterUserRequest
        {
            Name = email,
            Email = email,
            Password = password,
            ConfirmPassword = password
        };

        HttpResponseMessage registerResponse = await httpClient
            .PostAsJsonAsync(Routes.Auth.Register, registerUserRequest);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginUserRequest = new LoginUserRequest
        {
            Email = email,
            Password = password
        };

        HttpResponseMessage loginResponse = await httpClient
            .PostAsJsonAsync(Routes.Auth.Login, loginUserRequest);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        TokenDto? token = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();

        Assert.NotNull(token);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var createHabitRequest = new CreateHabitRequest
        {
            Name = "filip",
            Frequency = new FrequencyDto
            {
                TimesPerPeriod = 1,
                Type = FrequencyType.Daily
            },
            Target = new TargetDto
            {
                Unit = "sessions",
                Value = 30
            },
            Type = HabitType.Measurable
        };

        HttpResponseMessage createHabitResponse = await httpClient
            .PostAsJsonAsync(Routes.Habits.Create, createHabitRequest);

        Assert.Equal(HttpStatusCode.Created, createHabitResponse.StatusCode);

        HabitDto? createdHabit = await createHabitResponse.Content.ReadFromJsonAsync<HabitDto>();

        Assert.NotNull(createdHabit);
        Assert.Equal(createHabitRequest.Name, createdHabit.Name);

        var createTagRequest = new CreateTagRequest
        {
            Name = "Tag 1"
        };

        HttpResponseMessage createTagResponse = await httpClient
            .PostAsJsonAsync(Routes.Tags.Create, createTagRequest);

        Assert.Equal(HttpStatusCode.Created, createTagResponse.StatusCode);

        TagDto? createdTag = await createTagResponse.Content.ReadFromJsonAsync<TagDto>();

        Assert.NotNull(createdTag);
        Assert.Equal(createTagRequest.Name, createdTag.Name);

        var upsertHabitTagsRequest = new UpsertHabitTagsRequest
        {
            TagIds = [createdTag.Id]
        };

        HttpResponseMessage upsertTagsResponse = await httpClient
            .PutAsJsonAsync(Routes.HabitTags.UpsertTags(createdHabit.Id), upsertHabitTagsRequest);

        Assert.Equal(HttpStatusCode.NoContent, upsertTagsResponse.StatusCode);

        HttpResponseMessage getHabitResponse = await httpClient.GetAsync(Routes.Habits.GetById(createdHabit.Id));

        Assert.Equal(HttpStatusCode.OK, getHabitResponse.StatusCode);

        HabitWithTagsDto? habitWithTags = await getHabitResponse.Content.ReadFromJsonAsync<HabitWithTagsDto>();

        Assert.NotNull(habitWithTags);
        Assert.Single(habitWithTags.Tags);
        Assert.Equal(createdTag.Name, habitWithTags.Tags[0]);
    }
}

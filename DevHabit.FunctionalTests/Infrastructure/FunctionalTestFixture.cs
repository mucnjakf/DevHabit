using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WireMock.Server;

namespace DevHabit.FunctionalTests.Infrastructure;

public abstract class FunctionalTestFixture(DevHabitWebAppFactory factory) : IClassFixture<DevHabitWebAppFactory>
{
    private HttpClient? _authorizedClient;

    public HttpClient CreateClient() => factory.CreateClient();

    public WireMockServer WireMockServer => factory.GetWireMockServer();

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = "test@test.com",
        string password = "Test123!")
    {
        if (_authorizedClient is not null)
        {
            return _authorizedClient;
        }

        HttpClient client = CreateClient();

        bool userExists;

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            await using DevHabitDbContext dbContext = scope.ServiceProvider.GetRequiredService<DevHabitDbContext>();
            userExists = await dbContext.Users.AnyAsync(x => x.Email == email);
        }

        if (!userExists)
        {
            HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
                Routes.Auth.Register,
                new RegisterUserRequest
                {
                    Email = email,
                    Password = password,
                    ConfirmPassword = password,
                    Name = "Test"
                });

            registerResponse.EnsureSuccessStatusCode();
        }

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(Routes.Auth.Login, new LoginUserRequest
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        TokenDto? token = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();

        if (token?.AccessToken is null)
        {
            throw new InvalidOperationException("Failed to get access token");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _authorizedClient = client;

        return client;
    }

    public async Task CleanupDatabaseAsync()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        string? connectionString = configuration.GetConnectionString("Default");

        if (connectionString is null)
        {
            throw new InvalidOperationException("Database connection string not found in configuration");
        }

        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        await using NpgsqlCommand command = new(@"
            DO $$
            BEGIN
                TRUNCATE TABLE dev_habit.entries CASCADE;
                TRUNCATE TABLE dev_habit.entry_import_jobs CASCADE;
                TRUNCATE TABLE dev_habit.tags CASCADE;
                TRUNCATE TABLE dev_habit.habits CASCADE;
                TRUNCATE TABLE dev_habit.users CASCADE;

                TRUNCATE TABLE identity.asp_net_users CASCADE;
                TRUNCATE TABLE identity.refresh_tokens CASCADE;
            END $$;", connection);

        await command.ExecuteNonQueryAsync();
    }
}

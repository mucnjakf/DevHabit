using DevHabit.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Extensions;

internal static class DatabaseExtensions
{
    internal static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        await using ApplicationDbContext applicationDbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await using ApplicationIdentityDbContext applicationIdentityDbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationIdentityDbContext>();

        try
        {
            await applicationDbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Application database migrations applied successfully");

            await applicationIdentityDbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Identity database migrations applied successfully");
        }
        catch (Exception e)
        {
            app.Logger.LogError(e, "An error while applying database migrations");
            throw;
        }
    }
}

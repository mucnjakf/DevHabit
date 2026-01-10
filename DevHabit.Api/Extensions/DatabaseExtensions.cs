using DevHabit.Api.Constants;
using DevHabit.Api.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Extensions;

public static class DatabaseExtensions
{
    extension(WebApplication app)
    {
        public async Task ApplyMigrationsAsync()
        {
            using IServiceScope scope = app.Services.CreateScope();

            await using DevHabitDbContext devHabitDbContext = scope.ServiceProvider
                .GetRequiredService<DevHabitDbContext>();

            await using DevHabitIdentityDbContext devHabitIdentityDbContext = scope.ServiceProvider
                .GetRequiredService<DevHabitIdentityDbContext>();

            try
            {
                await devHabitDbContext.Database.MigrateAsync();
                app.Logger.LogInformation("Application database migrations applied successfully");

                await devHabitIdentityDbContext.Database.MigrateAsync();
                app.Logger.LogInformation("Identity database migrations applied successfully");
            }
            catch (Exception e)
            {
                app.Logger.LogError(e, "An error while applying database migrations");
                throw;
            }
        }

        public async Task SeedInitialDataAsync()
        {
            using IServiceScope scope = app.Services.CreateScope();

            RoleManager<IdentityRole>? roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            try
            {
                if (!await roleManager.RoleExistsAsync(Roles.Admin))
                {
                    await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
                }

                if (!await roleManager.RoleExistsAsync(Roles.Member))
                {
                    await roleManager.CreateAsync(new IdentityRole(Roles.Member));
                }

                app.Logger.LogInformation("Seeding initial data finished successfully");
            }
            catch (Exception e)
            {
                app.Logger.LogError(e, "An error occurred while seeding initial data");
            }
        }
    }
}

using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Database;

public sealed class DevHabitDbContext(DbContextOptions<DevHabitDbContext> options) : DbContext(options)
{
    public DbSet<Habit> Habits { get; set; }

    public DbSet<Tag> Tags { get; set; }

    public DbSet<HabitTag> HabitTags { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<GitHubPat> GitHubPats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Application);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DevHabitDbContext).Assembly);
    }
}

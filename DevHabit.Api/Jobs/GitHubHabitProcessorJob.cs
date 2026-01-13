using DevHabit.Api.Database;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Entities;
using DevHabit.Api.Enums;
using DevHabit.Api.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Jobs;

[DisallowConcurrentExecution]
public sealed class GitHubHabitProcessorJob(
    DevHabitDbContext dbContext,
    GitHubPatService gitHubPatService,
    GitHubService gitHubService,
    ILogger<GitHubHabitProcessorJob> logger) : IJob
{
    private const string PushEventType = "PushEvent";

    public async Task Execute(IJobExecutionContext context)
    {
        string habitId = context.JobDetail.JobDataMap.GetString("habitId")
                         ?? throw new InvalidOperationException("HabitId not found in job data");

        try
        {
            logger.LogInformation("Processing GitHub events for habit {HabitId}", habitId);

            Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(
                x => x.Id == habitId &&
                     x.AutomationSource == AutomationSource.GitHub &&
                     !x.IsArchived,
                context.CancellationToken);

            if (habit is null)
            {
                logger.LogWarning("Habit {HabitId} not found or no longer configured for GitHub automation", habitId);
                return;
            }

            string? gitHubPat = await gitHubPatService.GetAsync(habit.UserId, context.CancellationToken);

            if (string.IsNullOrWhiteSpace(gitHubPat))
            {
                logger.LogWarning("No GitHub PAT found for user {UserId}", habit.UserId);
                return;
            }

            GitHubUserProfileDto? profile =
                await gitHubService.GetUserProfileAsync(gitHubPat, context.CancellationToken);

            if (profile is null)
            {
                logger.LogWarning("Could not retrieve GitHub profile for user {UserId}", habit.UserId);
                return;
            }

            List<GitHubEventDto> gitHubEvents = [];
            const int perPage = 100;
            const int pagesToFetch = 10;

            for (int page = 1; page <= pagesToFetch; page++)
            {
                IReadOnlyList<GitHubEventDto>? pageEvents = await gitHubService
                    .GetUserEventsAsync(profile.Login, gitHubPat, page, perPage, context.CancellationToken);

                if (pageEvents is null || !pageEvents.Any())
                {
                    break;
                }

                gitHubEvents.AddRange(pageEvents);
            }

            if (!gitHubEvents.Any())
            {
                logger.LogWarning("Could not retrieve GitHub events for user {UserId}", habit.UserId);
                return;
            }

            var pushEvents = gitHubEvents.Where(x => x.Type == PushEventType).ToList();

            logger.LogInformation("Found {Count} push events for habit {HabitId}", pushEvents.Count, habitId);

            foreach (GitHubEventDto gitHubEvent in pushEvents)
            {
                bool exists = await dbContext.Entries.AnyAsync(
                    x => x.HabitId == habitId &&
                         x.ExternalId == gitHubEvent.Id,
                    context.CancellationToken);

                if (exists)
                {
                    logger.LogDebug("Entry already exists for event {EventId}", gitHubEvent.Id);
                    continue;
                }

                Entry entry = new()
                {
                    Id = $"e_{Guid.CreateVersion7()}",
                    HabitId = habitId,
                    UserId = habit.UserId,
                    Value = 1,
                    Notes = $"""
                                 {gitHubEvent.Actor.Login} pushed:

                                 {string.Join(
                                     Environment.NewLine,
                                     gitHubEvent.Payload.Commits?.Select(x => $"- {x.Message}") ?? [])}
                             """,
                    Source = EntrySource.Automation,
                    ExternalId = gitHubEvent.Id,
                    Date = DateOnly.FromDateTime(gitHubEvent.CreatedAt.DateTime),
                    CreatedAtUtc = DateTime.UtcNow,
                };

                await dbContext.Entries.AddAsync(entry);
                logger.LogInformation("Created entry for event {EventId} on habit {HabitId}", gitHubEvent.Id, habitId);
            }

            await dbContext.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("Completed processing GitHub events for habit {HabitId}", habitId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GitHub events for habit {HabitId}", habitId);
            throw;
        }
    }
}

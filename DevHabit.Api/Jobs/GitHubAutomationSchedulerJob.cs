using DevHabit.Api.Database;
using DevHabit.Api.Entities;
using DevHabit.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Jobs;

[DisallowConcurrentExecution]
public sealed class GitHubAutomationSchedulerJob(
    DevHabitDbContext dbContext,
    ILogger<GitHubAutomationSchedulerJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogInformation("Starting GitHub automation scheduler job");

            List<Habit> habitsToProcess = await dbContext.Habits
                .Where(x => x.AutomationSource == AutomationSource.GitHub && !x.IsArchived)
                .ToListAsync(context.CancellationToken);

            logger.LogInformation("Found {Count} habits with GitHub automation", habitsToProcess.Count);

            foreach (Habit habit in habitsToProcess)
            {
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity($"github-habit-{habit.Id}", "github-habits")
                    .StartNow()
                    .Build();

                IJobDetail jobDetail = JobBuilder.Create<GitHubHabitProcessorJob>()
                    .WithIdentity($"github-habit-{habit.Id}", "github-habits")
                    .UsingJobData("habitId", habit.Id)
                    .Build();

                await context.Scheduler.ScheduleJob(jobDetail, trigger);
                logger.LogInformation("Scheduled processor job for habit {HabitId}", habit.Id);
            }

            logger.LogInformation("Completed GitHub automation scheduler job");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scheduling GitHub automation scheduler job");
            throw;
        }
    }
}

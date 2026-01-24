using DevHabit.Api.Database;
using DevHabit.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Jobs;

public sealed class CleanupEntryImportJobsJob(DevHabitDbContext dbContext, ILogger<CleanupEntryImportJobsJob> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            DateTime completedJobsCutoffDate = DateTime.UtcNow.AddDays(-7);

            int deletedCountCompleted = await dbContext.EntryImportJobs
                .Where(x => x.Status == EntryImportStatus.Completed)
                .Where(x => x.CompletedAtUtc < completedJobsCutoffDate)
                .ExecuteDeleteAsync();

            if (deletedCountCompleted > 0)
            {
                logger.LogInformation("Deleted {DeletedCount} old entry import jobs", deletedCountCompleted);
            }

            DateTime failedJobsCutoffDate = DateTime.UtcNow.AddDays(-30);

            int deletedCountFailed = await dbContext.EntryImportJobs
                .Where(x => x.Status == EntryImportStatus.Failed)
                .Where(x => x.CompletedAtUtc < failedJobsCutoffDate)
                .ExecuteDeleteAsync();

            if (deletedCountFailed > 0)
            {
                logger.LogInformation("Deleted {DeletedCount} old entry import jobs", deletedCountFailed);
            }

            DateTime processingJobsCutoffDate = DateTime.UtcNow.AddHours(-2);

            int deletedCountProcessing = await dbContext.EntryImportJobs
                .Where(x => x.Status == EntryImportStatus.Processing)
                .Where(x => x.CreatedAtUtc < processingJobsCutoffDate)
                .ExecuteDeleteAsync();

            if (deletedCountProcessing > 0)
            {
                logger.LogInformation("Deleted {DeletedCount} old entry import jobs", deletedCountProcessing);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error cleaning up old import jobs");
        }
    }
}

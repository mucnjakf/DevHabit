using System.Globalization;
using CsvHelper;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Entities;
using DevHabit.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Jobs;

public sealed class ProcessEntryImportJob(DevHabitDbContext dbContext, ILogger<ProcessEntryImportJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string? importJobId = context.MergedJobDataMap.GetString("importJobId");

        EntryImportJob? entryImportJob = await dbContext.EntryImportJobs
            .FirstOrDefaultAsync(x => x.Id == importJobId);

        if (entryImportJob is null)
        {
            logger.LogError("Import job with ID {ImportJobId} not found", importJobId);
            return;
        }

        try
        {
            entryImportJob.Status = EntryImportStatus.Processing;
            await dbContext.SaveChangesAsync();

            using var memoryStream = new MemoryStream(entryImportJob.FileContent);
            using var streamReader = new StreamReader(memoryStream);
            using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);

            var csvEntryRecords = csv.GetRecords<CsvEntryRecord>().ToList();

            entryImportJob.TotalRecords = csvEntryRecords.Count;
            await dbContext.SaveChangesAsync();

            foreach (CsvEntryRecord csvEntryRecord in csvEntryRecords)
            {
                try
                {
                    Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(x =>
                        x.Id == csvEntryRecord.HabitId &&
                        x.UserId == entryImportJob.UserId);

                    if (habit is null)
                    {
                        throw new InvalidOperationException(
                            $"Habit with ID '{csvEntryRecord.HabitId}' not found or does not belong to the user");
                    }

                    var entry = new Entry
                    {
                        Id = $"e_{Guid.CreateVersion7()}",
                        UserId = entryImportJob.UserId,
                        HabitId = csvEntryRecord.HabitId,
                        Value = habit.Target.Value,
                        Date = csvEntryRecord.Date,
                        Notes = csvEntryRecord.Notes,
                        Source = EntrySource.FileImport,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    await dbContext.AddAsync(entry);
                    entryImportJob.SuccessfulRecords++;
                }
                catch (Exception e)
                {
                    entryImportJob.FailedRecords++;
                    entryImportJob.Errors.Add($"Error processing record: {e.Message}");

                    if (entryImportJob.Errors.Count >= 100)
                    {
                        entryImportJob.Errors.Add("Too many errors, stopping error collection...");
                        break;
                    }
                }
                finally
                {
                    entryImportJob.ProcessedRecords++;
                }

                if (entryImportJob.ProcessedRecords % 100 == 0)
                {
                    await dbContext.SaveChangesAsync();
                }
            }

            entryImportJob.Status = EntryImportStatus.Completed;
            entryImportJob.CompletedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing import job {ImportJobId}", importJobId);

            entryImportJob.Status = EntryImportStatus.Failed;
            entryImportJob.Errors.Add($"Fatal error: {e.Message}");
            entryImportJob.CompletedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }
}

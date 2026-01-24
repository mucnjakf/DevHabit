using DevHabit.Api.Entities;
using DevHabit.Api.Enums;
using DevHabit.Api.Services.Sorting;

namespace DevHabit.Api.Dtos.Entries;

public static class EntryMappings
{
    public static readonly SortMappingDefinition<EntryDto, Entry> SortMapping = new()
    {
        Mapping =
        [
            new SortMapping(nameof(EntryDto.Id), nameof(Entry.Id)),
            new SortMapping(nameof(EntryDto.Value), nameof(Entry.Value)),
            new SortMapping(nameof(EntryDto.Date), nameof(Entry.Date)),
            new SortMapping(nameof(EntryDto.CreatedAtUtc), nameof(Entry.CreatedAtUtc)),
            new SortMapping(nameof(EntryDto.UpdatedAtUtc), nameof(Entry.UpdatedAtUtc)),
        ],
    };

    public static Entry ToEntity(this CreateEntryRequest request, string userId)
    {
        return new Entry
        {
            Id = $"e_{Guid.CreateVersion7()}",
            HabitId = request.HabitId,
            UserId = userId,
            Value = request.Value,
            Notes = request.Notes,
            Source = EntrySource.Manual,
            ExternalId = null,
            IsArchived = false,
            Date = request.Date,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public static EntryDto ToDto(this Entry entry)
    {
        return new EntryDto
        {
            Id = entry.Id,
            Value = entry.Value,
            Notes = entry.Notes,
            Source = entry.Source,
            ExternalId = entry.ExternalId,
            IsArchived = entry.IsArchived,
            Date = entry.Date,
            CreatedAtUtc = entry.CreatedAtUtc,
            UpdatedAtUtc = entry.UpdatedAtUtc,
        };
    }

    public static void UpdateFromRequest(this Entry entry, UpdateEntryRequest request)
    {
        entry.Value = request.Value;
        entry.Notes = request.Notes;
        entry.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static EntryImportJobDto ToDto(this EntryImportJob entryImportJob)
    {
        return new EntryImportJobDto
        {
            Id = entryImportJob.Id,
            UserId = entryImportJob.UserId,
            Status = entryImportJob.Status,
            FileName = entryImportJob.FileName,
            TotalRecords = entryImportJob.TotalRecords,
            ProcessedRecords = entryImportJob.ProcessedRecords,
            SuccessfulRecords = entryImportJob.SuccessfulRecords,
            FailedRecords = entryImportJob.FailedRecords,
            CreatedAtUtc = entryImportJob.CreatedAtUtc,
        };
    }
}

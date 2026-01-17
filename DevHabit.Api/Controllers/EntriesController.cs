using System.Dynamic;
using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("entries")]
[Authorize(Roles = Roles.Member)]
[ApiVersion(1.0)]
public sealed class EntriesController(DevHabitDbContext dbContext, LinkService linkService, UserContext userContext)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginationDto<EntryDto>>> GetEntries(
        [FromHeader] string? accept,
        [FromQuery] EntriesQueryParameters parameters,
        [FromServices] SortMappingProvider sortMappingProvider,
        [FromServices] DataShapingService dataShapingService)
    {
        string userId = (await userContext.GetUserIdAsync())!;

        if (!sortMappingProvider.ValidateMappings<EntryDto, Entry>(parameters.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter is not valid: '{parameters.Sort}'");
        }

        if (!dataShapingService.Validate<EntryDto>(parameters.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: '{parameters.Fields}'");
        }

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<EntryDto, Entry>();

        IQueryable<EntryDto> query = dbContext.Entries
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => parameters.HabitId == null || x.HabitId == parameters.HabitId)
            .Where(x => parameters.FromDate == null || x.Date >= parameters.FromDate)
            .Where(x => parameters.ToDate == null || x.Date <= parameters.ToDate)
            .Where(x => parameters.Source == null || x.Source == parameters.Source)
            .Where(x => parameters.IsArchived == null || x.IsArchived == parameters.IsArchived)
            .ApplySort(parameters.Sort, sortMappings)
            .Select(EntryProjections.ProjectToDto());

        int totalCount = await query.CountAsync();

        List<EntryDto> entries = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        bool includeLinks = accept is VendorMediaTypeNames.Application.HateoasJson;

        var paginationDto = new PaginationDto<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(
                entries,
                parameters.Fields,
                includeLinks ? x => CreateLinksForEntry(x.Id, parameters.Fields) : null),
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };

        if (includeLinks)
        {
            paginationDto.Links = CreateLinksForEntries(
                parameters,
                paginationDto.HasNextPage,
                paginationDto.HasPreviousPage);
        }

        return Ok(paginationDto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEntry(
        [FromRoute] string id,
        [FromQuery] string? fields,
        [FromHeader(Name = "Accept")] string? accept,
        [FromServices] DataShapingService dataShapingService)
    {
        string userId = (await userContext.GetUserIdAsync())!;

        if (!dataShapingService.Validate<EntryDto>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: '{fields}'");
        }

        EntryDto? entry = await dbContext.Entries
            .AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(EntryProjections.ProjectToDto())
            .FirstOrDefaultAsync();

        if (entry is null)
        {
            return NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(entry, fields);

        if (accept is VendorMediaTypeNames.Application.HateoasJson)
        {
            List<LinkDto> links = CreateLinksForEntry(id, fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return Ok(shapedHabitDto);
    }

    [HttpPost]
    public async Task<ActionResult<EntryDto>> CreateEntry(
        [FromBody] CreateEntryRequest createEntryRequest,
        [FromHeader(Name = "accept")] string? accept,
        [FromServices] IValidator<CreateEntryRequest> validator)
    {
        await validator.ValidateAndThrowAsync(createEntryRequest);

        string userId = (await userContext.GetUserIdAsync())!;

        Habit? habit = await dbContext.Habits
            .FirstOrDefaultAsync(x => x.Id == createEntryRequest.HabitId && x.UserId == userId);

        if (habit is null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: "The specified habit does not exist");
        }

        Entry entry = createEntryRequest.ToEntity(userId);

        await dbContext.Entries.AddAsync(entry);

        await dbContext.SaveChangesAsync();

        EntryDto entryDto = entry.ToDto();

        if (accept is VendorMediaTypeNames.Application.HateoasJson)
        {
            entryDto.Links = CreateLinksForEntry(entry.Id);
        }

        return CreatedAtAction(nameof(GetEntry), new { id = entryDto.Id }, entryDto);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<List<EntryDto>>> CreateEntryBatch(
        [FromBody] CreateEntryBatchRequest createEntryBatchRequest,
        [FromHeader(Name = "accept")] string? accept,
        [FromServices] IValidator<CreateEntryBatchRequest> validator)
    {
        await validator.ValidateAndThrowAsync(createEntryBatchRequest);

        string userId = (await userContext.GetUserIdAsync())!;

        var habitIds = createEntryBatchRequest.Entries
            .Select(x => x.HabitId)
            .ToHashSet();

        List<Habit> existingHabits = await dbContext.Habits
            .Where(x => habitIds.Contains(x.Id) && x.UserId == userId)
            .ToListAsync();

        if (existingHabits.Count != habitIds.Count)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "One or more specified habit IDs are invalid");
        }

        var entries = createEntryBatchRequest.Entries
            .Select(x => x.ToEntity(userId))
            .ToList();

        await dbContext.Entries.AddRangeAsync(entries);
        await dbContext.SaveChangesAsync();

        var entryDtos = entries.Select(x => x.ToDto()).ToList();

        if (accept is VendorMediaTypeNames.Application.HateoasJson)
        {
            entryDtos.ForEach(x => x.Links = CreateLinksForEntry(x.Id, null, x.IsArchived));
        }

        return CreatedAtAction(nameof(GetEntries), entryDtos);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateEntry(
        [FromRoute] string id,
        [FromBody] UpdateEntryRequest updateEntryRequest,
        [FromServices] IValidator<UpdateEntryRequest> validator)
    {
        await validator.ValidateAndThrowAsync(updateEntryRequest);

        string userId = (await userContext.GetUserIdAsync())!;

        Entry? entry = await dbContext.Entries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        entry.UpdateFromRequest(updateEntryRequest);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/archive")]
    public async Task<ActionResult> ArchiveEntry([FromRoute] string id)
    {
        string userId = (await userContext.GetUserIdAsync())!;

        Entry? entry = await dbContext.Entries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        entry.IsArchived = true;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/unarchive")]
    public async Task<ActionResult> UnarchiveEntry([FromRoute] string id)
    {
        string userId = (await userContext.GetUserIdAsync())!;

        Entry? entry = await dbContext.Entries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        entry.IsArchived = false;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEntry([FromRoute] string id)
    {
        string userId = (await userContext.GetUserIdAsync())!;

        Entry? entry = await dbContext.Entries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        dbContext.Remove(entry);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<EntryStatsDto>> GetStats()
    {
        string userId = (await userContext.GetUserIdAsync())!;

        var entries = await dbContext.Entries
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Date)
            .Select(x => new { x.Date })
            .ToListAsync();

        if (!entries.Any())
        {
            return Ok(new EntryStatsDto
            {
                DailyStats = [],
                TotalEntries = 0,
                CurrentStreak = 0,
                LongestStreak = 0
            });
        }

        var dailyStats = entries
            .GroupBy(x => x.Date)
            .Select(x => new EntryDailyStatDto
            {
                Date = x.Key,
                Count = x.Count()
            })
            .OrderByDescending(x => x.Date)
            .ToList();

        int totalEntries = entries.Count;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var dates = entries
            .Select(x => x.Date)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        int currentStreak = 0;
        int longestStreak = 0;
        int currentCount = 0;

        for (int i = dates.Count - 1; i >= 0; i--)
        {
            if (i == dates.Count - 1)
            {
                if (dates[i] == today)
                {
                    currentStreak = 1;
                }
                else
                {
                    break;
                }
            }
            else if (dates[i].AddDays(1) == dates[i + 1])
            {
                currentStreak++;
            }
            else
            {
                break;
            }
        }

        for (int i = 0; i < dates.Count; i++)
        {
            if (i == 0 || dates[i] == dates[i - 1].AddDays(1))
            {
                currentCount++;
                longestStreak = Math.Max(longestStreak, currentCount);
            }
            else
            {
                currentCount = 1;
            }
        }

        return Ok(new EntryStatsDto
        {
            DailyStats = dailyStats,
            TotalEntries = totalEntries,
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak
        });
    }

    private List<LinkDto> CreateLinksForEntry(string id, string? fields = null, bool isArchived = false)
    {
        return
        [
            linkService.Create(nameof(GetEntry), "self", HttpMethods.Get, new { id, fields }),
            linkService.Create(nameof(UpdateEntry), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(DeleteEntry), "delete", HttpMethods.Delete, new { id }),

            isArchived
                ? linkService.Create(nameof(UnarchiveEntry), "unarchive", HttpMethods.Put, new { id })
                : linkService.Create(nameof(ArchiveEntry), "archive", HttpMethods.Put, new { id }),
        ];
    }

    private List<LinkDto> CreateLinksForEntries(
        EntriesQueryParameters parameters,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetEntries), "self", HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                sort = parameters.Sort,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                page = parameters.Page,
                page_size = parameters.PageSize,
            }),
            linkService.Create(nameof(GetStats), "stats", HttpMethods.Get),
            linkService.Create(nameof(CreateEntry), "create", HttpMethods.Post),
            linkService.Create(nameof(CreateEntryBatch), "create-batch", HttpMethods.Post),
        ];

        if (hasPreviousPage)
        {
            links.Add(linkService.Create(nameof(GetEntries), "previous-page", HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                sort = parameters.Sort,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                page = parameters.Page - 1,
                page_size = parameters.PageSize,
            }));
        }

        if (hasNextPage)
        {
            links.Add(linkService.Create(nameof(GetEntries), "next-page", HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                sort = parameters.Sort,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                page = parameters.Page + 1,
                page_size = parameters.PageSize,
            }));
        }

        return links;
    }
}

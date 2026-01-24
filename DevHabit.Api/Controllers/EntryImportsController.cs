using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Entities;
using DevHabit.Api.Enums;
using DevHabit.Api.Jobs;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Controllers;

[Authorize(Roles = Roles.Member)]
[ApiController]
[Route("entries/imports")]
[ApiVersion(1.0)]
public sealed class EntryImportsController(
    DevHabitDbContext dbContext,
    ISchedulerFactory schedulerFactory,
    UserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginationDto<EntryImportJobDto>>> GetEntryImportJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        string? userId = await userContext.GetUserIdAsync();

        IOrderedQueryable<EntryImportJob> query = dbContext.EntryImportJobs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc);

        int totalCount = await query.CountAsync();

        List<EntryImportJobDto> entryImportJobDtos = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(EntryProjections.ProjectToEntryImportJobDto())
            .ToListAsync();

        var result = new PaginationDto<EntryImportJobDto>
        {
            Items = entryImportJobDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EntryImportJobDto>> GetEntryImportJob([FromRoute] string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        EntryImportJobDto? entryImportJobDto = await dbContext.EntryImportJobs
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(EntryProjections.ProjectToEntryImportJobDto())
            .FirstOrDefaultAsync();

        if (entryImportJobDto is null)
        {
            return NotFound();
        }

        return Ok(entryImportJobDto);
    }

    [HttpPost]
    public async Task<ActionResult<EntryImportJobDto>> CreateImportJob(
        [FromForm] CreateEntryImportJobRequest createEntryImportJobRequest,
        [FromServices] IValidator<CreateEntryImportJobRequest> validator)
    {
        await validator.ValidateAndThrowAsync(createEntryImportJobRequest);

        string? userId = await userContext.GetUserIdAsync();

        using var memoryStream = new MemoryStream();
        await createEntryImportJobRequest.File.CopyToAsync(memoryStream);

        var entryImportJob = new EntryImportJob
        {
            Id = $"ei_{Guid.CreateVersion7()}",
            UserId = userId!,
            Status = EntryImportStatus.Pending,
            FileName = createEntryImportJobRequest.File.FileName,
            FileContent = memoryStream.ToArray(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await dbContext.EntryImportJobs.AddAsync(entryImportJob);
        await dbContext.SaveChangesAsync();

        IScheduler scheduler = await schedulerFactory.GetScheduler();

        IJobDetail jobDetail = JobBuilder.Create<ProcessEntryImportJob>()
            .WithIdentity($"process-entry-import-{entryImportJob.Id}")
            .UsingJobData("importJobId", entryImportJob.Id)
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity($"process-entry-import-trigger-{entryImportJob.Id}")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);

        EntryImportJobDto entryImportJobDto = entryImportJob.ToDto();

        return CreatedAtAction(nameof(GetEntryImportJob), new { id = entryImportJobDto.Id }, entryImportJobDto);
    }
}

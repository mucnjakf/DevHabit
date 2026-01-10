using System.Dynamic;
using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Hateoas;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("habits")]
[ApiVersion(1.0)]
[Authorize(Roles = Roles.Member)]
public sealed class HabitsController(
    DevHabitDbContext dbContext,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHabits(
        [FromHeader(Name = "Accept")] string accept,
        [FromQuery] HabitsQueryParameters parameters,
        [FromServices] SortMappingProvider sortMappingProvider,
        [FromServices] DataShapingService dataShapingService)
    {
        string userId = await userContext.GetUserIdAsync();

        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(parameters.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter is not valid: '{parameters.Sort}'");
        }

        if (!dataShapingService.Validate<HabitDto>(parameters.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: '{parameters.Fields}'");
        }

        parameters.Search ??= parameters.Search?.Trim().ToLower();

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

        IQueryable<HabitDto> query = dbContext.Habits
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x =>
                parameters.Search == null ||
                x.Name.ToLower().Contains(parameters.Search) ||
                x.Description != null && x.Description.ToLower().Contains(parameters.Search))
            .Where(x => parameters.Type == null || x.Type == parameters.Type)
            .Where(x => parameters.Status == null || x.Status == parameters.Status)
            .ApplySort(parameters.Sort, sortMappings)
            .Select(HabitProjections.ProjectToDto());

        int totalCount = await query.CountAsync();

        List<HabitDto> habits = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        bool includeLinks = accept is VendorMediaTypeNames.Application.HateoasJson;

        var paginationResult = new PaginationDto<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(
                habits,
                parameters.Fields,
                includeLinks ? x => CreateLinksForHabit(x.Id, parameters.Fields) : null),
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };

        if (includeLinks)
        {
            paginationResult.Links = CreateLinksForHabits(
                parameters,
                paginationResult.HasNextPage,
                paginationResult.HasPreviousPage);
        }

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    [ApiVersion(1)]
    public async Task<IActionResult> GetHabit(
        [FromRoute] string id,
        [FromQuery] string? fields,
        [FromHeader(Name = "Accept")] string? accept,
        [FromServices] DataShapingService dataShapingService)
    {
        string userId = await userContext.GetUserIdAsync();

        if (!dataShapingService.Validate<HabitWithTagsDto>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: '{fields}'");
        }

        HabitWithTagsDto? habit = await dbContext.Habits
            .AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(HabitProjections.ProjectToDtoWithTags())
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, fields);

        if (accept is VendorMediaTypeNames.Application.HateoasJson)
        {
            List<LinkDto> links = CreateLinksForHabit(id, fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return Ok(shapedHabitDto);
    }

    [HttpGet("{id}")]
    [ApiVersion(2)]
    public async Task<IActionResult> GetHabitV2(
        [FromHeader(Name = "Accept")] string accept,
        [FromRoute] string id,
        [FromQuery] string? fields,
        [FromServices] DataShapingService dataShapingService)
    {
        string userId = await userContext.GetUserIdAsync();

        if (!dataShapingService.Validate<HabitWithTagsDtoV2>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: '{fields}'");
        }

        HabitWithTagsDtoV2? habit = await dbContext.Habits
            .AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(HabitProjections.ProjectToDtoWithTagsV2())
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, fields);

        if (accept is VendorMediaTypeNames.Application.HateoasJson)
        {
            List<LinkDto> links = CreateLinksForHabit(id, fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return Ok(shapedHabitDto);
    }

    // TODO: return response object
    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        [FromBody] CreateHabitRequest createHabitRequest,
        [FromServices] IValidator<CreateHabitRequest> validator)
    {
        await validator.ValidateAndThrowAsync(createHabitRequest);

        string userId = await userContext.GetUserIdAsync();

        Habit habit = createHabitRequest.ToEntity(userId!);

        await dbContext.Habits.AddAsync(habit);

        await dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();
        habitDto.Links = CreateLinksForHabit(habit.Id, null);

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(
        [FromRoute] string id,
        [FromBody] UpdateHabitRequest updateHabitRequest,
        [FromServices] IValidator<UpdateHabitRequest> validator)
    {
        await validator.ValidateAndThrowAsync(updateHabitRequest);

        string userId = await userContext.GetUserIdAsync();

        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (habit is null)
        {
            return NotFound();
        }

        habit.UpdateFromRequest(updateHabitRequest);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(
        [FromRoute] string id,
        [FromBody] JsonPatchDocument<HabitDto> patchDocument)
    {
        string? userId = await userContext.GetUserIdAsync();

        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (habit is null)
        {
            return NotFound();
        }

        HabitDto habitDto = habit.ToDto();

        patchDocument.ApplyTo(habitDto, ModelState);

        if (!TryValidateModel(habitDto))
        {
            return ValidationProblem();
        }

        habit.Name = habitDto.Name;
        habit.Description = habitDto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHabit([FromRoute] string id)
    {
        string userId = await userContext.GetUserIdAsync();

        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (habit is null)
        {
            return NotFound();
        }

        dbContext.Remove(habit);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private List<LinkDto> CreateLinksForHabits(HabitsQueryParameters parameters, bool hasNextPage, bool hasPreviousPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetHabits), "self", HttpMethods.Get, new
            {
                page = parameters.Page,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type,
                status = parameters.Status
            }),
            linkService.Create(nameof(CreateHabit), "create", HttpMethods.Post),
        ];

        if (hasNextPage)
        {
            links.Add(linkService.Create(nameof(GetHabits), "next-page", HttpMethods.Get, new
            {
                page = parameters.Page + 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type,
                status = parameters.Status
            }));
        }

        if (hasPreviousPage)
        {
            links.Add(linkService.Create(nameof(GetHabits), "previous-page", HttpMethods.Get, new
            {
                page = parameters.Page - 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type,
                status = parameters.Status
            }));
        }

        return links;
    }

    private List<LinkDto> CreateLinksForHabit(string id, string? fields)
    {
        return
        [
            linkService.Create(nameof(GetHabit), "self", HttpMethods.Get, new { id, fields }),
            linkService.Create(nameof(UpdateHabit), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(PatchHabit), "partial-update", HttpMethods.Patch, new { id }),
            linkService.Create(nameof(DeleteHabit), "delete", HttpMethods.Delete, new { id }),

            linkService.Create(
                nameof(HabitTagsController.UpsertHabitTags),
                "upsert-tags",
                HttpMethods.Put,
                new { habitId = id },
                HabitTagsController.Name)
        ];
    }
}

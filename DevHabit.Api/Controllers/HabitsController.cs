using System.Linq.Dynamic.Core;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("habits")]
public sealed class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HabitsCollectionDto>> GetHabits(
        [FromQuery] HabitsQueryParameters query,
        SortMappingProvider sortMappingProvider)
    {
        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter is not valid: '{query.Sort}'");
        }

        query.Search ??= query.Search?.Trim().ToLower();

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

        List<HabitDto> habits = await dbContext.Habits
            .Where(x =>
                query.Search == null ||
                x.Name.ToLower().Contains(query.Search) ||
                x.Description != null && x.Description.ToLower().Contains(query.Search))
            .Where(x => query.Type == null || x.Type == query.Type)
            .Where(x => query.Status == null || x.Status == query.Status)
            .ApplySort(query.Sort, sortMappings)
            .Select(HabitQueries.ProjectToDto())
            .ToListAsync();

        var habitsCollection = new HabitsCollectionDto
        {
            Data = habits
        };

        return Ok(habitsCollection);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HabitWithTagsDto>> GetHabit(string id)
    {
        HabitWithTagsDto? habit = await dbContext.Habits
            .Where(x => x.Id == id)
            .Select(HabitQueries.ProjectToDtoWithTags())
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        return Ok(habit);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator)
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

        Habit habit = createHabitDto.ToEntity();

        dbContext.Habits.Add(habit);

        await dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(string id, UpdateHabitDto updateHabitDto)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(x => x.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        habit.UpdateFromDto(updateHabitDto);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(x => x.Id == id);

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
    public async Task<ActionResult> DeleteHabit(string id)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(x => x.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        dbContext.Remove(habit);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}

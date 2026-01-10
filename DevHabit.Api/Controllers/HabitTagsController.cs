using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.HabitTags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("habits/{habitId}/tags")]
[ApiVersion(1.0)]
[Authorize(Roles = Roles.Member)]
public sealed class HabitTagsController(DevHabitDbContext dbContext, UserContext userContext) : ControllerBase
{
    public static readonly string Name = nameof(HabitTagsController).Replace("Controller", string.Empty);

    [HttpPut]
    public async Task<ActionResult> UpsertHabitTags(
        [FromRoute] string habitId,
        [FromBody] UpsertHabitTagsRequest upsertHabitTagsRequest,
        [FromServices] IValidator<UpsertHabitTagsRequest> validator)
    {
        await validator.ValidateAndThrowAsync(upsertHabitTagsRequest);

        string userId = await userContext.GetUserIdAsync();

        Habit? habit = await dbContext.Habits
            .Where(x => x.UserId == userId)
            .Include(x => x.HabitTags)
            .FirstOrDefaultAsync(x => x.Id == habitId);

        if (habit is null)
        {
            return NotFound();
        }

        var currentTagIds = habit.HabitTags.Select(x => x.TagId).ToHashSet();

        if (currentTagIds.SetEquals(upsertHabitTagsRequest.TagIds))
        {
            return NoContent();
        }

        List<string> existingTagIds = await dbContext.Tags
            .Where(x => upsertHabitTagsRequest.TagIds.Contains(x.Id) && x.UserId == userId)
            .Select(x => x.Id)
            .ToListAsync();

        if (existingTagIds.Count != upsertHabitTagsRequest.TagIds.Count)
        {
            return BadRequest("One or more tag IDs is invalid");
        }

        habit.HabitTags.RemoveAll(x => !upsertHabitTagsRequest.TagIds.Contains(x.TagId));

        string[] tagIdsToAdd = upsertHabitTagsRequest.TagIds.Except(currentTagIds).ToArray();

        habit.HabitTags.AddRange(tagIdsToAdd.Select(tagId => new HabitTag
        {
            HabitId = habitId,
            TagId = tagId,
            CreatedAtUtc = DateTime.UtcNow
        }));

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{tagId}")]
    public async Task<ActionResult> DeleteHabitTag([FromRoute] string habitId, [FromRoute] string tagId)
    {
        string userId = await userContext.GetUserIdAsync();

        bool habitExists = await dbContext.Habits
            .AsNoTracking()
            .AnyAsync(x => x.Id == habitId && x.UserId == userId);

        bool tagExists = await dbContext.Tags
            .AsNoTracking()
            .AnyAsync(x => x.Id == tagId && x.UserId == userId);

        if (!habitExists || !tagExists)
        {
            return NotFound();
        }

        HabitTag? habitTag = await dbContext.HabitTags
            .SingleOrDefaultAsync(x => x.HabitId == habitId && x.TagId == tagId);

        if (habitTag is null)
        {
            return NotFound();
        }

        dbContext.HabitTags.Remove(habitTag);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}

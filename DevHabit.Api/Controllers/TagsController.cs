using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Hateoas;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("tags")]
[Authorize(Roles = Roles.Member)]
[ApiVersion(1.0)]
public sealed class TagsController(
    DevHabitDbContext dbContext,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{
    // TODO: return response object
    [HttpGet]
    public async Task<ActionResult<TagsCollectionDto>> GetTags([FromHeader] string? accept)
    {
        string userId = await userContext.GetUserIdAsync();

        List<TagDto> tags = await dbContext.Tags
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(TagProjections.ProjectToDto())
            .ToListAsync();

        var habitsCollectionDto = new TagsCollectionDto
        {
            Items = tags
        };

        if (accept is VendorMediaTypeNames.Application.HateoasJson)
        {
            habitsCollectionDto.Links = CreateLinksForTags();
        }

        return Ok(habitsCollectionDto);
    }

    // TODO: return response object
    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag([FromRoute] string id, [FromHeader] string? accept)
    {
        string? userId = await userContext.GetUserIdAsync();

        TagDto? tag = await dbContext.Tags
            .AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(TagProjections.ProjectToDto())
            .FirstOrDefaultAsync();

        if (tag is null)
        {
            return NotFound();
        }

        if (accept is VendorMediaTypeNames.Application.HateoasJson)
        {
            tag.Links = CreateLinksForTag(id);
        }

        return Ok(tag);
    }

    // TODO: return response object
    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag(
        [FromBody] CreateTagRequest createTagRequest,
        [FromServices] IValidator<CreateTagRequest> validator)
    {
        await validator.ValidateAndThrowAsync(createTagRequest);

        string userId = await userContext.GetUserIdAsync();

        Tag tag = createTagRequest.ToEntity(userId!);

        if (await dbContext.Tags.AnyAsync(x => x.Name == tag.Name))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"The tag '{tag.Name}' already exists");
        }

        dbContext.Tags.Add(tag);

        await dbContext.SaveChangesAsync();

        TagDto tagDto = tag.ToDto();

        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTag(
        [FromRoute] string id,
        [FromBody] UpdateTagRequest updateTagRequest,
        [FromServices] IValidator<UpdateTagRequest> validator)
    {
        await validator.ValidateAndThrowAsync(updateTagRequest);

        string userId = await userContext.GetUserIdAsync();

        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (tag is null)
        {
            return NotFound();
        }

        tag.UpdateFromRequest(updateTagRequest);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag([FromRoute] string id)
    {
        string userId = await userContext.GetUserIdAsync();

        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (tag is null)
        {
            return NotFound();
        }

        dbContext.Remove(tag);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private List<LinkDto> CreateLinksForTags()
    {
        return
        [
            linkService.Create(nameof(GetTags), "self", HttpMethods.Get),
            linkService.Create(nameof(CreateTag), "create", HttpMethods.Post),
        ];
    }

    private List<LinkDto> CreateLinksForTag(string id)
    {
        return
        [
            linkService.Create(nameof(GetTag), "self", HttpMethods.Get, new { id }),
            linkService.Create(nameof(CreateTag), "create", HttpMethods.Post),
            linkService.Create(nameof(UpdateTag), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(DeleteTag), "delete", HttpMethods.Delete, new { id })
        ];
    }
}

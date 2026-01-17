using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ResponseCache(Duration = 120)]
[ApiController]
[Route("tags")]
[Authorize(Roles = Roles.Member)]
[ApiVersion(1.0)]
public sealed class TagsController(
    DevHabitDbContext dbContext,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginationDto<TagDto>>> GetTags(
        [FromHeader] string? accept,
        [FromQuery] TagsQueryParameters parameters)
    {
        string userId = (await userContext.GetUserIdAsync())!;

        IQueryable<TagDto> query = dbContext.Tags
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(TagProjections.ProjectToDto());

        int totalCount = await query.CountAsync();

        List<TagDto> tags = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        bool includeLinks = accept is VendorMediaTypeNames.Application.HateoasJson;

        if (includeLinks)
        {
            tags.ForEach(x => x.Links = CreateLinksForTag(x.Id));
        }

        var paginationDto = new PaginationDto<TagDto>
        {
            Items = tags,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };

        if (includeLinks)
        {
            paginationDto.Links = CreateLinksForTags(
                parameters,
                paginationDto.HasNextPage,
                paginationDto.HasPreviousPage);
        }

        return Ok(paginationDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag([FromRoute] string id, [FromHeader] string? accept)
    {
        string userId = (await userContext.GetUserIdAsync())!;

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

    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag(
        [FromBody] CreateTagRequest createTagRequest,
        [FromServices] IValidator<CreateTagRequest> validator)
    {
        await validator.ValidateAndThrowAsync(createTagRequest);

        string userId = (await userContext.GetUserIdAsync())!;

        Tag tag = createTagRequest.ToEntity(userId);

        if (await dbContext.Tags.AnyAsync(x => x.Name == tag.Name))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"The tag '{tag.Name}' already exists");
        }

        dbContext.Tags.Add(tag);

        await dbContext.SaveChangesAsync();

        TagDto tagDto = tag.ToDto();
        tagDto.Links = CreateLinksForTag(tagDto.Id);

        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTag(
        [FromRoute] string id,
        [FromBody] UpdateTagRequest updateTagRequest,
        [FromServices] IValidator<UpdateTagRequest> validator,
        [FromServices] InMemoryETagStore inMemoryETagStore)
    {
        await validator.ValidateAndThrowAsync(updateTagRequest);

        string userId = (await userContext.GetUserIdAsync())!;

        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (tag is null)
        {
            return NotFound();
        }

        tag.UpdateFromRequest(updateTagRequest);

        await dbContext.SaveChangesAsync();

        inMemoryETagStore.SetETag(Request.Path.Value!, tag.ToDto());

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag([FromRoute] string id)
    {
        string userId = (await userContext.GetUserIdAsync())!;

        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (tag is null)
        {
            return NotFound();
        }

        dbContext.Remove(tag);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private List<LinkDto> CreateLinksForTags(TagsQueryParameters parameters, bool hasNextPage, bool hasPreviousPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetTags), "self", HttpMethods.Get, new
            {
                page = parameters.Page,
                pageSize = parameters.PageSize
            }),
            linkService.Create(nameof(CreateTag), "create", HttpMethods.Post),
        ];

        if (hasNextPage)
        {
            links.Add(linkService.Create(nameof(GetTags), "next-page", HttpMethods.Get, new
            {
                page = parameters.Page + 1,
                pageSize = parameters.PageSize
            }));
        }

        if (hasPreviousPage)
        {
            links.Add(linkService.Create(nameof(GetTags), "previous-page", HttpMethods.Get, new
            {
                page = parameters.Page - 1,
                pageSize = parameters.PageSize
            }));
        }

        return links;
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

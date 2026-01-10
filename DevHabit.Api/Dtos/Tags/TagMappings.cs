using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Tags;

public static class TagMappings
{
    public static TagDto ToDto(this Tag tag)
    {
        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            CreatedAtUtc = tag.CreatedAtUtc,
            UpdatedAtUtc = tag.UpdatedAtUtc
        };
    }

    public static Tag ToEntity(this CreateTagRequest request, string userId)
    {
        return new Tag
        {
            Id = $"t_{Guid.CreateVersion7()}",
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static void UpdateFromRequest(this Tag tag, UpdateTagRequest request)
    {
        tag.Name = request.Name;
        tag.Description = request.Description;
        tag.UpdatedAtUtc = DateTime.UtcNow;
    }
}

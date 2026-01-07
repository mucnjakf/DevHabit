using System.Linq.Expressions;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Tags;

public static class TagProjections
{
    public static Expression<Func<Tag, TagDto>> ProjectToDto()
    {
        return x => new TagDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        };
    }
}

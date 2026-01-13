using System.Linq.Expressions;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Entries;

public static class EntryProjections
{
    public static Expression<Func<Entry, EntryDto>> ProjectToDto()
    {
        return x => new EntryDto
        {
            Id = x.Id,
            Value = x.Value,
            Notes = x.Notes,
            Source = x.Source,
            ExternalId = x.ExternalId,
            IsArchived = x.IsArchived,
            Date = x.Date,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
        };
    }
}

using System.Linq.Expressions;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Users;

public static class UserQueries
{
    public static Expression<Func<User, UserDto>> ProjectToDto()
    {
        return x => new UserDto
        {
            Id = x.Id,
            Email = x.Email,
            Name = x.Name,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
        };
    }
}

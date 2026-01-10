using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Users;

public static class UserMappings
{
    public static User ToEntity(this RegisterUserRequest request)
    {
        return new User
        {
            Id = $"u_{Guid.CreateVersion7()}",
            Name = request.Name,
            Email = request.Email,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

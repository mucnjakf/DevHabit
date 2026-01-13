using Microsoft.AspNetCore.Identity;

namespace DevHabit.Api.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public string Value { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }

    public string UserId { get; set; } = null!;

    public IdentityUser User { get; set; } = null!;
}

using FluentValidation;

namespace DevHabit.Api.Dtos.Auth;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Refresh token value is required");
    }
}

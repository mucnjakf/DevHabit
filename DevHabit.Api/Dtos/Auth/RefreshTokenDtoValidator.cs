using FluentValidation;

namespace DevHabit.Api.Dtos.Auth;

public sealed class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Refresh token value is required");
    }
}

using FluentValidation;

namespace DevHabit.Api.Dtos.Auth;

public sealed class LoginUserDtoValidator : AbstractValidator<LoginUserDto>
{
    public LoginUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(300)
            .WithMessage("User email must be between 3 and 300 characters");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}

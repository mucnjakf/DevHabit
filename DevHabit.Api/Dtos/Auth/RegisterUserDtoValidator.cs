using FluentValidation;

namespace DevHabit.Api.Dtos.Auth;

public sealed class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(300)
            .WithMessage("User email must be between 3 and 300 characters");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100)
            .WithMessage("User name must be between 3 and 100 characters");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Confirm password must be the same as password");
    }
}

using FluentValidation;

namespace DevHabit.Api.Dtos.Users;

public sealed class UpdateCurrentUserRequestValidator : AbstractValidator<UpdateCurrentUserRequest>
{
    public UpdateCurrentUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Current user name must not be empty");
    }
}

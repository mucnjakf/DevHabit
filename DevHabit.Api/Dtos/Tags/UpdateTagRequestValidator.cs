using FluentValidation;

namespace DevHabit.Api.Dtos.Tags;

public sealed class UpdateTagRequestValidator : AbstractValidator<UpdateTagRequest>
{
    public UpdateTagRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3);

        RuleFor(x => x.Description)
            .MaximumLength(50)
            .When(x => x.Description is not null)
            .WithMessage("Tag description cannot exceed 500 characters");
    }
}

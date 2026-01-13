using FluentValidation;

namespace DevHabit.Api.Dtos.Entries;

public sealed class UpdateEntryRequestValidator : AbstractValidator<UpdateEntryRequest>
{
    public UpdateEntryRequestValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Entry value must not be empty")
            .GreaterThanOrEqualTo(0)
            .WithMessage("Entry value must be greater than or equal to 0");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null)
            .WithMessage("Entry notes must not exceed 1000 characters");
    }
}

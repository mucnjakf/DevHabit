using FluentValidation;

namespace DevHabit.Api.Dtos.Entries;

public sealed class CreateEntryRequestValidator : AbstractValidator<CreateEntryRequest>
{
    public CreateEntryRequestValidator()
    {
        RuleFor(x => x.HabitId)
            .NotEmpty()
            .WithMessage("Habit ID must not be empty");

        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Entry value must not be empty")
            .GreaterThanOrEqualTo(0)
            .WithMessage("Entry value must be greater than or equal to 0");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null)
            .WithMessage("Entry notes must not exceed 1000 characters");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Entry date must not be empty")
            .Must(x => x <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Entry date can not be in the future");
    }
}

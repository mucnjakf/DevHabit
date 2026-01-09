using FluentValidation;

namespace DevHabit.Api.Dtos.GitHub;

public sealed class StoreGitHubPatDtoValidator : AbstractValidator<StoreGitHubPatDto>
{
    public StoreGitHubPatDtoValidator()
    {
        RuleFor(x => x.Pat)
            .NotEmpty()
            .WithMessage("GitHub PAT is required");

        RuleFor(x => x.ExpiresInDays)
            .GreaterThan(0)
            .WithMessage("GitHub expiration days must be greater than 0");
    }
}

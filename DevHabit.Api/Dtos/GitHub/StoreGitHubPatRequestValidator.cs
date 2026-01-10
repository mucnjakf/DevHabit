using FluentValidation;

namespace DevHabit.Api.Dtos.GitHub;

public sealed class StoreGitHubPatRequestValidator : AbstractValidator<StoreGitHubPatRequest>
{
    public StoreGitHubPatRequestValidator()
    {
        RuleFor(x => x.Pat)
            .NotEmpty()
            .WithMessage("GitHub PAT is required");

        RuleFor(x => x.ExpiresInDays)
            .GreaterThan(0)
            .WithMessage("GitHub expiration days must be greater than 0");
    }
}

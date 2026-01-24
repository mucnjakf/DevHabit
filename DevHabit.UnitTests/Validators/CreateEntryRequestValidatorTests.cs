using DevHabit.Api.Dtos.Entries;
using FluentValidation.Results;

namespace DevHabit.UnitTests.Validators;

public sealed class CreateEntryRequestValidatorTests
{
    private readonly CreateEntryRequestValidator _sut = new();

    [Fact]
    public async Task ValidateAsync_ShouldSucceed_WhenInputRequestIsValid()
    {
        var request = new CreateEntryRequest
        {
            HabitId = $"h_{Guid.CreateVersion7()}",
            Value = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        ValidationResult validationResult = await _sut.ValidateAsync(request);

        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenHabitIdIsEmpty()
    {
        var request = new CreateEntryRequest
        {
            HabitId = string.Empty,
            Value = 1,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        ValidationResult validationResult = await _sut.ValidateAsync(request);

        Assert.False(validationResult.IsValid);
        ValidationFailure validationFailure = Assert.Single(validationResult.Errors);
        Assert.Equal(nameof(CreateEntryRequest.HabitId), validationFailure.PropertyName);
    }
}

using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.UserSearchModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<UserSearchModelValidator>
{
    [Fact]
    public async Task ShouldNotHaveValidationError_WhenToDateIsAfterFromDate()
    {
        // Arrange
        var model = new UserSearchModel
        {
            FromDay = "01",
            FromMonth = "01",
            FromYear = "2024",
            ToDay = "15",
            ToMonth = "01",
            ToYear = "2024"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }

    [Fact]
    public async Task ShouldHaveValidationError_WhenToDateIsBeforeFromDate()
    {
        // Arrange
        var model = new UserSearchModel
        {
            FromDay = "15",
            FromMonth = "01",
            FromYear = "2024",
            ToDay = "01",
            ToMonth = "01",
            ToYear = "2024"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrors()
            .WithErrorMessage("'Search to' date must be after 'Search from' date");
    }

    [Fact]
    public async Task ShouldNotHaveValidationError_WhenToDateEqualsFromDate()
    {
        // Arrange
        var model = new UserSearchModel
        {
            FromDay = "15",
            FromMonth = "01",
            FromYear = "2024",
            ToDay = "15",
            ToMonth = "01",
            ToYear = "2024"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }

    [Theory]
    [InlineData(null, "01", "2024", "15", "01", "2024")] // Missing FromDay
    [InlineData("01", null, "2024", "15", "01", "2024")] // Missing FromMonth
    [InlineData("01", "01", null, "15", "01", "2024")] // Missing FromYear
    [InlineData("01", "01", "2024", null, "01", "2024")] // Missing ToDay
    [InlineData("01", "01", "2024", "15", null, "2024")] // Missing ToMonth
    [InlineData("01", "01", "2024", "15", "01", null)] // Missing ToYear
    public async Task ShouldNotRunValidation_WhenDatePartsAreIncomplete(
        string? fromDay, string? fromMonth, string? fromYear,
        string? toDay, string? toMonth, string? toYear)
    {
        // Arrange
        var model = new UserSearchModel
        {
            FromDay = fromDay,
            FromMonth = fromMonth,
            FromYear = fromYear,
            ToDay = toDay,
            ToMonth = toMonth,
            ToYear = toYear
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert — no error even though date order might be wrong, because one is incomplete
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }
}
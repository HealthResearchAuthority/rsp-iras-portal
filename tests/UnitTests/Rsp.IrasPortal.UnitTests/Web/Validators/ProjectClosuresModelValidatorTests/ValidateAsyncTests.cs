using FluentValidation.TestHelper;
using Rsp.Portal.Web.Models;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.ProjectClosuresModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<ProjectClosuresModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForAllDatePartsMissing()
    {
        // Arrange
        var model = new ProjectClosuresModel
        {
            ActualClosureDateDay = "",
            ActualClosureDateMonth = "",
            ActualClosureDateYear = ""
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor("ActualClosureDate")
            .WithErrorMessage("Enter the project closure date");
    }

    [Theory]
    [InlineData("", "1", "2025", "Project closure must include a day")]
    [InlineData("1", "", "2025", "Project closure must include a month")]
    [InlineData("1", "1", "", "Project closure must include a year")]
    [InlineData("", "", "2025", "Project closure must include a day and month")]
    [InlineData("", "1", "", "Project closure must include a day and year")]
    [InlineData("1", "", "", "Project closure must include a month and year")]
    [InlineData("", "", "", "Enter the project closure date")] // covered by first test too; ok to keep or remove
    public async Task ShouldHaveValidationErrorForMissingDateParts(
        string day,
        string month,
        string year,
        string expectedMessage)
    {
        // Arrange
        var model = new ProjectClosuresModel
        {
            ActualClosureDateDay = day,
            ActualClosureDateMonth = month,
            ActualClosureDateYear = year
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor("ActualClosureDate")
            .WithErrorMessage(expectedMessage);
    }

    [Theory]
    // Non-numeric
    [InlineData("aa", "1", "2025")]
    [InlineData("1", "bb", "2025")]
    [InlineData("1", "1", "cccc")]
    // Out of range
    [InlineData("0", "1", "2025")]
    [InlineData("32", "1", "2025")]
    [InlineData("1", "0", "2025")]
    [InlineData("1", "13", "2025")]
    [InlineData("1", "1", "0")]
    // Not a real date (DaysInMonth)
    [InlineData("30", "2", "2025")]
    [InlineData("29", "2", "2023")]
    [InlineData("31", "4", "2025")]
    public async Task ShouldHaveValidationErrorForInvalidRealDate(string day, string month, string year)
    {
        // Arrange
        var model = new ProjectClosuresModel
        {
            ActualClosureDateDay = day,
            ActualClosureDateMonth = month,
            ActualClosureDateYear = year
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor("ActualClosureDate")
            .WithErrorMessage("Project closure must be a real date");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForFutureDate()
    {
        // Arrange
        var tomorrow = DateTime.Today.AddDays(1);

        var model = new ProjectClosuresModel
        {
            ActualClosureDateDay = tomorrow.Day.ToString(),
            ActualClosureDateMonth = tomorrow.Month.ToString(),
            ActualClosureDateYear = tomorrow.Year.ToString()
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor("ActualClosureDate")
            .WithErrorMessage("Project closure must be today or in the past");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForToday()
    {
        // Arrange
        var today = DateTime.Today;

        var model = new ProjectClosuresModel
        {
            ActualClosureDateDay = today.Day.ToString(),
            ActualClosureDateMonth = today.Month.ToString(),
            ActualClosureDateYear = today.Year.ToString()
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor("ActualClosureDate");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForPastDate()
    {
        // Arrange
        var past = DateTime.Today.AddDays(-1);

        var model = new ProjectClosuresModel
        {
            ActualClosureDateDay = past.Day.ToString(),
            ActualClosureDateMonth = past.Month.ToString(),
            ActualClosureDateYear = past.Year.ToString()
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor("ActualClosureDate");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForLeapDayOnLeapYear()
    {
        // Arrange
        var model = new ProjectClosuresModel
        {
            ActualClosureDateDay = "29",
            ActualClosureDateMonth = "2",
            ActualClosureDateYear = "2024"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor("ActualClosureDate");
    }
}
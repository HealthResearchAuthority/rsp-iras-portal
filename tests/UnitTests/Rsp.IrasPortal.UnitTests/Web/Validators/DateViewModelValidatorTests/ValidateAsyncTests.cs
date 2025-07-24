using FluentValidation;
using FluentValidation.TestHelper;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.DateViewModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<DateViewModelValidator>
{
    [Fact]
    public async Task ShouldNotHaveValidationError_WhenDateIsTodayOrFuture()
    {
        // Arrange
        var today = DateTime.Now.Date;
        var model = new DateViewModel
        {
            Day = today.Day.ToString(),
            Month = today.Month.ToString(),
            Year = today.Year.ToString()
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ShouldHaveValidationError_WhenDateIsInPast()
    {
        // Arrange
        var yesterday = DateTime.Now.Date.AddDays(-1);
        var model = new DateViewModel
        {
            Day = yesterday.Day.ToString(),
            Month = yesterday.Month.ToString(),
            Year = yesterday.Year.ToString()
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorMessage("The date should be a valid date and in future");
    }

    [Theory]
    [InlineData(null, "01", "2024")]
    [InlineData("01", null, "2024")]
    [InlineData("01", "01", null)]
    [InlineData("32", "01", "2024")]
    [InlineData("01", "13", "2024")]
    [InlineData("01", "01", "abcd")]
    public async Task ShouldHaveValidationError_WhenDateIsInvalid(string? day, string? month, string? year)
    {
        // Arrange
        var model = new DateViewModel
        {
            Day = day,
            Month = month,
            Year = year
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorMessage("The date should be a valid date and in future");
    }

    [Fact]
    public async Task ShouldUseCustomValidationMessageAndPropertyName()
    {
        // Arrange
        var yesterday = DateTime.Now.Date.AddDays(-1);
        var model = new DateViewModel
        {
            Day = yesterday.Day.ToString(),
            Month = yesterday.Month.ToString(),
            Year = yesterday.Year.ToString()
        };
        var context = new ValidationContext<DateViewModel>(model);
        context.RootContextData[ValidationKeys.ValidationMessage] = "Custom error message";
        context.RootContextData[ValidationKeys.PropertyName] = "CustomDate";

        // Act
        var result = await Sut.TestValidateAsync(context);

        // Assert
        result
            .ShouldHaveValidationErrorFor("CustomDate")
            .WithErrorMessage("Custom error message");
    }
}
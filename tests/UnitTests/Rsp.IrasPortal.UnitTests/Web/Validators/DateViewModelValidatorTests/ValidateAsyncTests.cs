using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.DateViewModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<DateViewModelValidator>
{
    [Fact]
    public async Task ShouldNotHaveValidationError_WhenDateIsToday()
    {
        // Arrange: today (valid)
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
    public async Task ShouldNotHaveValidationError_WhenDateIsPast()
    {
        // Arrange: yesterday (valid)
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
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ShouldHaveValidationError_WhenDateIsFuture()
    {
        // Arrange: tomorrow (invalid)
        var tomorrow = DateTime.Now.Date.AddDays(1);
        var model = new DateViewModel
        {
            Day = tomorrow.Day.ToString(),
            Month = tomorrow.Month.ToString(),
            Year = tomorrow.Year.ToString()
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorMessage("The date should be a valid date and it must be present or past date");
    }
}
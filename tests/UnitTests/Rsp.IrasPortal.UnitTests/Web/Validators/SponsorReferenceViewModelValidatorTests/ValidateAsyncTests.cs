using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.SponsorReferenceViewModelTests;

public class ValidateAsyncTests : TestServiceBase<SponsorReferenceViewModelValidator>
{
    [Fact]
    public async Task ShouldNotHaveValidationError_WhenDateIsTodayOrPast()
    {
        // Arrange
        var today = DateTime.Now.Date;
        var model = new SponsorReferenceViewModel
        {
            SponsorModificationDate = new DateViewModel
            {
                Day = today.Day.ToString(),
                Month = today.Month.ToString(),
                Year = today.Year.ToString()
            },
            MainChangesDescription = "Valid description"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ShouldHaveValidationError_WhenDateIsInFuture()
    {
        // Arrange
        var futureDate = DateTime.Now.Date.AddDays(1);
        var model = new SponsorReferenceViewModel
        {
            SponsorModificationDate = new DateViewModel
            {
                Day = futureDate.Day.ToString(),
                Month = futureDate.Month.ToString(),
                Year = futureDate.Year.ToString()
            },
            MainChangesDescription = "Valid description"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor("SponsorModificationDate")
            .WithErrorMessage("The date should be a valid date and not in future");
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
        var model = new SponsorReferenceViewModel
        {
            SponsorModificationDate = new DateViewModel
            {
                Day = day,
                Month = month,
                Year = year
            },
            MainChangesDescription = "Valid description"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor("SponsorModificationDate")
            .WithErrorMessage("The date should be a valid date and not in future");
    }

    [Fact]
    public async Task ShouldHaveValidationError_WhenMainChangesDescriptionIsEmpty()
    {
        // Arrange
        var today = DateTime.Now.Date;
        var model = new SponsorReferenceViewModel
        {
            SponsorModificationDate = new DateViewModel
            {
                Day = today.Day.ToString(),
                Month = today.Month.ToString(),
                Year = today.Year.ToString()
            },
            MainChangesDescription = ""
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.MainChangesDescription)
            .WithErrorMessage("Enter a job description");
    }

    [Fact]
    public async Task ShouldHaveValidationError_WhenMainChangesDescriptionIsTooLong()
    {
        // Arrange
        var today = DateTime.Now.Date;
        var longText = new string('A', 3201);
        var model = new SponsorReferenceViewModel
        {
            SponsorModificationDate = new DateViewModel
            {
                Day = today.Day.ToString(),
                Month = today.Month.ToString(),
                Year = today.Year.ToString()
            },
            MainChangesDescription = longText
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.MainChangesDescription)
            .WithErrorMessage("Job description must be 3200 characters or less");

        result
            .ShouldHaveValidationErrorFor("_DescriptionExcessCharactersCount")
            .WithErrorMessage("You have 1 character too many");
    }
}
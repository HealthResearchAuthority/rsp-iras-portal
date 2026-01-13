using FluentValidation.TestHelper;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.ProjectClosuresSearchModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<ProjectClosuresSearchModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForTooShortSearchTerm()
    {
        // Arrange
        var model = new ProjectClosuresSearchModel
        {
            SearchTerm = "1"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SearchTerm)
            .WithErrorMessage("IRAS ID must be at least 2 characters");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForTooLongSearchTerm()
    {
        // Arrange
        var model = new ProjectClosuresSearchModel
        {
            SearchTerm = "12345678"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SearchTerm)
            .WithErrorMessage("IRAS ID must be no more than 7 characters");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForStartingWithZero()
    {
        // Arrange
        var model = new ProjectClosuresSearchModel
        {
            SearchTerm = "0123456"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SearchTerm)
            .WithErrorMessage("IRAS ID cannot start with '0'");
    }

    [Theory]
    [InlineData("12A")]
    [InlineData("12/34")]
    [InlineData("123-456")]
    [InlineData("ABC")]
    [InlineData("123 456")]
    public async Task ShouldHaveValidationErrorForInvalidCharacters(string term)
    {
        // Arrange
        var model = new ProjectClosuresSearchModel
        {
            SearchTerm = term
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SearchTerm)
            .WithErrorMessage("IRAS ID must only contain numbers");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForEmptySearchTerm()
    {
        // Arrange
        var model = new ProjectClosuresSearchModel
        {
            SearchTerm = ""
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("123")]
    [InlineData("1234")]
    [InlineData("12345")]
    [InlineData("123456")]
    [InlineData("1234567")]
    public async Task ShouldNotHaveValidationErrorForValidSearchTerm(string validTerm)
    {
        // Arrange
        var model = new ProjectClosuresSearchModel
        {
            SearchTerm = validTerm
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
    }
}
using FluentValidation.TestHelper;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.SponsorAuthorisationsSearchModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<AuthorisationsModificationsSearchModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForTooShortSearchTerm()
    {
        // Arrange
        var model = new AuthorisationsModificationsSearchModel
        {
            SearchTerm = "1"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SearchTerm)
            .WithErrorMessage("Modification ID must be at least 2 characters");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForTooLongSearchTerm()
    {
        // Arrange
        var model = new AuthorisationsModificationsSearchModel
        {
            SearchTerm = "123456789"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SearchTerm)
            .WithErrorMessage("Modification ID must be 8 characters or less");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForInvalidCharacters()
    {
        // Arrange
        var model = new AuthorisationsModificationsSearchModel
        {
            SearchTerm = "12A"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SearchTerm)
            .WithErrorMessage("Modification ID must only include numbers and a slash, like 123456/1");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForEmptySearchTerm()
    {
        // Arrange
        var model = new AuthorisationsModificationsSearchModel
        {
            SearchTerm = ""
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForValidSearchTerm()
    {
        // Arrange
        var model = new AuthorisationsModificationsSearchModel
        {
            SearchTerm = "12/34"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
    }
}
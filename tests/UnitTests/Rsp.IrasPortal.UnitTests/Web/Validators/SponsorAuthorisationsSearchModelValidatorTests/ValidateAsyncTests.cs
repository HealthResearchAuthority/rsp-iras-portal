using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Features.Approvals.RecordSearch.Models;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.SponsorAuthorisationsSearchModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<SponsorAuthorisationsSearchModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForTooShortSearchTerm()
    {
        // Arrange
        var model = new SponsorAuthorisationsSearchModel
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
        var model = new SponsorAuthorisationsSearchModel
        {
            SearchTerm = "123456789"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SearchTerm)
            .WithErrorMessage("Modification ID must be no more than 8 characters");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForInvalidCharacters()
    {
        // Arrange
        var model = new SponsorAuthorisationsSearchModel
        {
            SearchTerm = "12A"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SearchTerm)
            .WithErrorMessage("Modification ID must only contain numbers and '/'");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForEmptySearchTerm()
    {
        // Arrange
        var model = new SponsorAuthorisationsSearchModel
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
        var model = new SponsorAuthorisationsSearchModel
        {
            SearchTerm = "12/34"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
    }
}
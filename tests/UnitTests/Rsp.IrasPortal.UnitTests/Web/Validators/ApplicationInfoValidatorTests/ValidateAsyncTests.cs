using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.ApplicationInfoValidatorTests;

public class ValidateAsyncTests : TestServiceBase<ApplicationInfoValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyName()
    {
        // Arrange
        var model = new ApplicationInfoViewModel
        {
            Name = string.Empty,
            Description = "Test Description"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyDescription()
    {
        // Arrange
        var model = new ApplicationInfoViewModel
        {
            Name = "Test Name",
            Description = string.Empty
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required");
    }

    [Fact]
    public async Task ShouldPassValidationWhenBothNameAndDescriptionAreProvided()
    {
        // Arrange
        var model = new ApplicationInfoViewModel
        {
            Name = "Test Application",
            Description = "This is a test description"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
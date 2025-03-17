using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.UserInfoValidatortests;

public class ValidateAsyncTests : TestServiceBase<UserInfoValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyName()
    {
        // Arrange
        var model = new UserViewModel
        {
            FirstName = null!,
            LastName = "Ham"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("Field is mandatory");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForCountryWhenRoleOperationsIsSelected()
    {
        // Arrange
        var model = new UserViewModel
        {
            Role = "operations",
            Country = null!
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("Field is mandatory when the role 'operations' is selected");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForCountryWhenRoleOperationsIsNotSelected()
    {
        // Arrange
        var model = new UserViewModel
        {
            Role = "admin",
            Country = null!
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Country);
    }

    [Fact]
    public async Task ShouldPassValidationWhenBothNameAndDescriptionAreProvided()
    {
        // Arrange
        var model = new UserViewModel
        {
            FirstName = "John",
            LastName = "Ham"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }
}
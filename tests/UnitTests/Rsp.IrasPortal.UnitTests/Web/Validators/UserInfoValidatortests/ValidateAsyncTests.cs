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
            GivenName = null!,
            FamilyName = "Ham"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.GivenName)
            .WithErrorMessage("Enter a first name");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForTelephoneNoDigit()
    {
        // Arrange
        var model = new UserViewModel
        {
            GivenName = "Hello",
            FamilyName = "Ham",
            Telephone = "qwertyuiopa"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Telephone)
            .WithErrorMessage("Telephone must only contain numbers");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForTelephone11DigitOrMore()
    {
        // Arrange
        var model = new UserViewModel
        {
            GivenName = "Hello",
            FamilyName = "Ham",
            Telephone = "078987654323"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Telephone)
            .WithErrorMessage("Telephone must be 11 digits or less");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForCountryWhenRoleOperationsIsSelected()
    {
        // Arrange
        var model = new UserViewModel
        {
            UserRoles = [ new()
            {
                 Name = "operations",
                 IsSelected = true
            }],
            Country = null!
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("You must provide a country");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForCountryWhenRoleOperationsIsNotSelected()
    {
        // Arrange
        var model = new UserViewModel
        {
            UserRoles =
            [ new()
                {
                    Name = "admin"
                }
            ],
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
            GivenName = "John",
            FamilyName = "Ham"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.GivenName);
        result.ShouldNotHaveValidationErrorFor(x => x.FamilyName);
    }
}
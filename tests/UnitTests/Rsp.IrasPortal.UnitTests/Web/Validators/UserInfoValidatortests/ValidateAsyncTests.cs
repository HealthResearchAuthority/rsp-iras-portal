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
            .WithErrorMessage("Enter a first name");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForTelephoneNoDigit()
    {
        // Arrange
        var model = new UserViewModel
        {
            FirstName = "Hello",
            LastName = "Ham",
            Telephone = "qwerty"
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
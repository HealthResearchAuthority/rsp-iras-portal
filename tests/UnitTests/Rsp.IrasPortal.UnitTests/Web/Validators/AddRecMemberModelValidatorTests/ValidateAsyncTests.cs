using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.AddRecMemberModelValidatorsTests;

public class ValidateAsyncTests : TestServiceBase<AddRecMemberModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForEmailAddressEmpty()
    {
        // Arrange
        var model = new AddRecMemberViewModel
        {
            Email = null!
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Enter an email address in the correct format, like name@example.com");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForInvalidEmailAddress()
    {
        // Arrange
        var model = new AddRecMemberViewModel
        {
            Email = "1245uuu1111122"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Enter an email address in the correct format, like name@example.com");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForLongEmailAddress()
    {
        // Arrange
        var model = new AddRecMemberViewModel
        {
            Email = "wewewewewefaddddddddddddddddddddddddddddddweweweweweweweweweweweweweweweweweweweweweweweweedddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrhellooooowijrieoifneuiofnieufnieufnbiuebfiuwbefuiwebfiuwefg@google.com"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email address must be 254 characters or less");
    }
}
using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.RecMemberModelValidatorsTests;

public class ValidateAsyncTests : TestServiceBase<RecMemberModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyCommitteeDesignationRole()
    {
        // Arrange
        var model = new RecMemberViewModel
        {
            CommitteeRole = null!,
            Designation = null!
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.CommitteeRole)
            .WithErrorMessage("Select a committee role");

        result
            .ShouldHaveValidationErrorFor(x => x.Designation)
            .WithErrorMessage("Select a designation");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForInvalidTelephoneNumber()
    {
        // Arrange
        var model = new RecMemberViewModel
        {
            RecTelephoneNumber = "0ttttt7898765432312232323dddddd1212121212121"
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.RecTelephoneNumber)
            .WithErrorMessage("Enter a telephone number, like 01632 960 001, 07700 900 982 or +44 808 157 0192");

        result
            .ShouldHaveValidationErrorFor(x => x.RecTelephoneNumber)
            .WithErrorMessage("Telephone must be 13 digits or less");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForDateTime()
    {
        // Arrange
        var model = new RecMemberViewModel
        {
            DateTimeLeftDay = "10",
            DateTimeLeftMonth = "5",
            DateTimeLeftYear = "2028",
            MemberLeftOrganisation = true
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.DateTimeLeft)
            .WithErrorMessage("The date the member left this committee must be today or in the past");
    }
}
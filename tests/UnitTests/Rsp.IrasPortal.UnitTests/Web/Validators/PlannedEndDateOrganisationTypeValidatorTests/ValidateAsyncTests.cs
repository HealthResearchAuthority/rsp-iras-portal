using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.PlannedEndDateOrganisationTypeValidatorTests;

public class ValidateAsyncTests : TestServiceBase<PlannedEndDateOrganisationTypeValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyName()
    {
        // Arrange
        var model = new PlannedEndDateOrganisationTypeViewModel
        {
            SelectedOrganisationTypes = null!
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SelectedOrganisationTypes)
            .WithErrorMessage("Select at least one organisation type.");
    }
}
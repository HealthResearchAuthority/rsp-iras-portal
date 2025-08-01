using FluentValidation;
using FluentValidation.TestHelper;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.AffectingOrganisationsViewModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<AffectingOrganisationsViewModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationError_WhenSelectedLocationsIsEmpty()
    {
        // Arrange
        var model = new AffectingOrganisationsViewModel
        {
            SelectedLocations = []
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SelectedLocations)
            .WithErrorMessage("Select at least one location");
    }

    [Fact]
    public async Task ShouldNotHaveValidationError_WhenSelectedLocationsIsNotEmpty()
    {
        // Arrange
        var model = new AffectingOrganisationsViewModel
        {
            SelectedLocations = ["England"]
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SelectedLocations);
    }

    [Fact]
    public async Task ShouldHaveValidationErrors_WhenAffectedOrgsDataValidationEnabled_AndFieldsAreEmpty()
    {
        // Arrange
        var model = new AffectingOrganisationsViewModel
        {
            SelectedLocations = ["England"],
            SelectedAffectedOrganisations = null,
            SelectedAdditionalResources = null
        };

        var context = new ValidationContext<AffectingOrganisationsViewModel>(model);
        context.RootContextData[ValidationKeys.ProjectModificationPlannedEndDate.AffectedOrganisations] = true;

        // Act
        var result = await Sut.TestValidateAsync(context);

        // Assert
        result
            .ShouldHaveValidationErrorFor(x => x.SelectedAffectedOrganisations)
            .WithErrorMessage("Select one option for affected organisations");

        result
            .ShouldHaveValidationErrorFor(x => x.SelectedAdditionalResources)
            .WithErrorMessage("Select one option for additional resources");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrors_WhenAffectedOrgsDataValidationEnabled_AndFieldsAreValid()
    {
        // Arrange
        var model = new AffectingOrganisationsViewModel
        {
            SelectedLocations = ["England"],
            SelectedAffectedOrganisations = "OPT0323",
            SelectedAdditionalResources = "OPT0004"
        };

        var context = new ValidationContext<AffectingOrganisationsViewModel>(model);
        context.RootContextData[ValidationKeys.ProjectModificationPlannedEndDate.AffectedOrganisations] = true;

        // Act
        var result = await Sut.TestValidateAsync(context);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SelectedAffectedOrganisations);
        result.ShouldNotHaveValidationErrorFor(x => x.SelectedAdditionalResources);
    }

    [Fact]
    public async Task ShouldNotValidateAffectedOrgsFields_WhenValidationFlagIsFalse()
    {
        // Arrange
        var model = new AffectingOrganisationsViewModel
        {
            SelectedLocations = ["England"],
            SelectedAffectedOrganisations = null,
            SelectedAdditionalResources = null
        };

        var context = new ValidationContext<AffectingOrganisationsViewModel>(model);
        context.RootContextData[ValidationKeys.ProjectModificationPlannedEndDate.AffectedOrganisations] = false;

        // Act
        var result = await Sut.TestValidateAsync(context);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SelectedAffectedOrganisations);
        result.ShouldNotHaveValidationErrorFor(x => x.SelectedAdditionalResources);
    }
}
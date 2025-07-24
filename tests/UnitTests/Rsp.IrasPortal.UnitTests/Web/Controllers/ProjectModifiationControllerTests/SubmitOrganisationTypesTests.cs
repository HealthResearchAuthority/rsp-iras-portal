using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class SubmitOrganisationTypesTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task SubmitOrganisationTypes_ReturnsRedirect_WhenModelIsValid()
    {
        // Arrange
        var model = new PlannedEndDateOrganisationTypeViewModel
        {
            SelectedOrganisationTypes = new List<string> { "OPT0025" }
        };

        Mocker
            .GetMock<IValidator<PlannedEndDateOrganisationTypeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), default))
            .ReturnsAsync(new ValidationResult()); // Valid

        // Act
        var result = await Sut.SubmitOrganisationTypes(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToRouteResult>(result);
        Assert.Equal("app:projectoverview", redirectResult.RouteName);
    }

    [Fact]
    public async Task SubmitOrganisationTypes_ReturnsView_WhenModelIsInvalid()
    {
        // Arrange
        var model = new PlannedEndDateOrganisationTypeViewModel();

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("SelectedOrganisationTypes", "Please select at least one organisation type.")
        };

        Mocker
            .GetMock<IValidator<PlannedEndDateOrganisationTypeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), default))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await Sut.SubmitOrganisationTypes(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("PlannedEndDateOrganisationType", viewResult.ViewName);
        Assert.False(Sut.ModelState.IsValid);
        Assert.True(Sut.ModelState.ContainsKey("SelectedOrganisationTypes"));
    }
}
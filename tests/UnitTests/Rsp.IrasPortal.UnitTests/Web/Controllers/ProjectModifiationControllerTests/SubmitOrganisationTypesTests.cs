using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
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
            SelectedOrganisationTypes = ["NHS/HSC"]
        };

        // Set up TempData with required value for published version
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            ["QuestionSetPublishedVersionId"] = "test-version-id"
        };

        Sut.TempData = tempData;

        // Set up HttpContext.Items for RespondentId (if SaveModificationAnswers is called)
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items["RespondentId"] = "test-respondent-id";

        // Mock SaveModificationAnswers to avoid null reference on HttpContext/TempData
        Mocker.GetMock<ProjectModificationController>().CallBase = true;
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Mocker
            .GetMock<IValidator<PlannedEndDateOrganisationTypeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), default))
            .ReturnsAsync(new ValidationResult()); // Valid

        // Act
        var result = await Sut.SubmitOrganisationTypes(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pmc:affectingorganisations");
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
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("PlannedEndDateOrganisationType");
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ContainsKey("SelectedOrganisationTypes").ShouldBeTrue();
    }
}
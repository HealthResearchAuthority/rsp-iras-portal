using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.OrganisationTypeModificationControllerTests;

public class SubmitOrganisationTypesTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task SubmitOrganisationTypes_ReturnsViewWithErrors_WhenValidationFails()
    {
        // Arrange
        var model = new PlannedEndDateOrganisationTypeViewModel();
        var validationFailures = new List<ValidationFailure>
        {
            new("SelectedOrganisationTypes", "Select at least one organisation type")
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
        viewResult.Model.ShouldBe(model);
        Sut.ModelState.ContainsKey("SelectedOrganisationTypes").ShouldBeTrue();
    }

    [Fact]
    public async Task SubmitOrganisationTypes_RedirectsToProjectOverview_WhenSaveForLater()
    {
        // Arrange
        var model = new PlannedEndDateOrganisationTypeViewModel
        {
            SelectedOrganisationTypes = ["NHS/HSC"]
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.QuestionSetPublishedVersionId] = "v1.0"
        };

        Mocker
            .GetMock<IValidator<PlannedEndDateOrganisationTypeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respId";

        // Act
        var result = await Sut.SubmitOrganisationTypes(model, saveForLater: true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");
    }

    [Fact]
    public async Task SubmitOrganisationTypes_RedirectsToAffectingOrganisations_WhenValidAndNotSaveForLater()
    {
        // Arrange
        var model = new PlannedEndDateOrganisationTypeViewModel
        {
            SelectedOrganisationTypes = ["NHS/HSC"]
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.QuestionSetPublishedVersionId] = "v1.0"
        };

        Mocker
            .GetMock<IValidator<PlannedEndDateOrganisationTypeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respId";

        // Act
        var result = await Sut.SubmitOrganisationTypes(model, saveForLater: false);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:affectingorganisations");
    }
}
using System.Text.Json;
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

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.AffectingOrganisationsControllerTests;

public class SaveAffectedOrganisationsResponsesTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task SaveAffectedOrganisationsResponses_ReturnsViewWithErrors_WhenValidationFails()
    {
        // Arrange
        var model = new AffectingOrganisationsViewModel
        {
            SelectedLocations = new List<string>(),
            SelectedAffectedOrganisations = null,
            SelectedAdditionalResources = null
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModificationPlannedEndDate.AffectingOrganisationsType] = JsonSerializer.Serialize(new List<string> { "NHS/HSC" })
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("SelectedLocations", "Select at least one location")
        };

        Mocker
            .GetMock<IValidator<AffectingOrganisationsViewModel>>()
            .Setup(v => v.Validate(It.IsAny<IValidationContext>()))
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await Sut.SaveAffectedOrganisationsResponses(model, saveForLater: false);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AffectingOrganisations");
        viewResult.Model.ShouldBe(model);
        Sut.ModelState.ContainsKey("SelectedLocations").ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAffectedOrganisationsResponses_RedirectsToProjectOverview_WhenSaveForLater()
    {
        // Arrange
        var model = new AffectingOrganisationsViewModel
        {
            SelectedLocations = new List<string> { "England" },
            SelectedAffectedOrganisations = "OPT0323",
            SelectedAdditionalResources = "OPT0004"
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.QuestionSetPublishedVersionId] = "v1.0"
        };

        Mocker
            .GetMock<IValidator<AffectingOrganisationsViewModel>>()
            .Setup(v => v.Validate(It.IsAny<IValidationContext>()))
            .Returns(new ValidationResult());

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respId";

        // Act
        var result = await Sut.SaveAffectedOrganisationsResponses(model, saveForLater: true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");
    }

    [Fact]
    public async Task SaveAffectedOrganisationsResponses_RedirectsToModificationChangesReview_WhenValidAndNotSaveForLater()
    {
        // Arrange
        var model = new AffectingOrganisationsViewModel
        {
            SelectedLocations = new List<string> { "England" },
            SelectedAffectedOrganisations = "OPT0323",
            SelectedAdditionalResources = "OPT0004"
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.QuestionSetPublishedVersionId] = "v1.0"
        };

        Mocker
            .GetMock<IValidator<AffectingOrganisationsViewModel>>()
            .Setup(v => v.Validate(It.IsAny<IValidationContext>()))
            .Returns(new ValidationResult());

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respId";

        // Act
        var result = await Sut.SaveAffectedOrganisationsResponses(model, saveForLater: false);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:modificationchangesreview");
    }
}
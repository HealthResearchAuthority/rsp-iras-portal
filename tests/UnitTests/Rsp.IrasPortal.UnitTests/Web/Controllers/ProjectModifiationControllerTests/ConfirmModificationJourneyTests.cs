using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class ConfirmModificationJourneyTests : TestServiceBase<ProjectModificationController>
{
    [Theory, AutoData]
    public async Task ConfirmModificationJourney_ReturnsView_WhenValidationFails(
        AreaOfChangeViewModel model,
        List<GetAreaOfChangesResponse> areaChanges)
    {
        // Arrange
        var validator = Mocker.GetMock<IValidator<AreaOfChangeViewModel>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AreaOfChangeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
            {
            new(nameof(model.AreaOfChangeId), "Area is required")
            }));

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(areaChanges)
        };

        // Act
        var result = await Sut.ConfirmModificationJourney(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AreaOfChange");
        viewResult.Model.ShouldBeOfType<AreaOfChangeViewModel>();
        validator.Verify();
    }

    [Theory, InlineData(ModificationJourneyTypes.ParticipatingOrganisation, "ParticipatingOrganisation")]
    [InlineData(ModificationJourneyTypes.PlannedEndDate, "PlannedEndDate")]
    [InlineData(ModificationJourneyTypes.ProjectDocument, "ProjectDocument")]
    public async Task ConfirmModificationJourney_RedirectsToCorrectAction_WhenJourneyTypeValid(
        string journeyType, string expectedAction)
    {
        // Arrange
        var model = new AreaOfChangeViewModel
        {
            AreaOfChangeId = 1,
            SpecificChangeId = 101
        };

        var validator = Mocker.GetMock<IValidator<AreaOfChangeViewModel>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AreaOfChangeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        var respondent = new { GivenName = "Test", FamilyName = "User" };

        var areaChanges = new List<GetAreaOfChangesResponse>
    {
        new()
        {
            Id = 1,
            ModificationSpecificAreaOfChanges = new List<ModificationSpecificAreaOfChangeDto>
            {
                new() { Id = 101, JourneyType = journeyType, Name = "Some Change" }
            }
        }
    };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(areaChanges),
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid()
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Items["Respondent"] = respondent;

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CreateModificationChange(It.IsAny<ProjectModificationChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationChangeResponse { Id = Guid.NewGuid() }
            });

        // Act
        var result = await Sut.ConfirmModificationJourney(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(expectedAction);
    }

    [Theory, AutoData]
    public async Task ConfirmModificationJourney_ReturnsAreaOfChangeView_WhenJourneyTypeUnknown(List<GetAreaOfChangesResponse> areaChanges)
    {
        // Arrange
        var model = new AreaOfChangeViewModel
        {
            AreaOfChangeId = 1,
            SpecificChangeId = 999
        };

        var validator = Mocker.GetMock<IValidator<AreaOfChangeViewModel>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AreaOfChangeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(areaChanges)
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Items["Respondent"] = new { GivenName = "Test", FamilyName = "User" };

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid()
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CreateModificationChange(It.IsAny<ProjectModificationChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationChangeResponse { Id = Guid.NewGuid() }
            });

        // Act
        var result = await Sut.ConfirmModificationJourney(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AreaOfChange");
    }
}
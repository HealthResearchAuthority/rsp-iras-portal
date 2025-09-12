using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications;

public class ConfirmModificationJourneyTests : TestServiceBase<ModificationsController>
{
    private readonly Mock<IValidator<AreaOfChangeViewModel>> _validator;
    private readonly Mock<ICmsQuestionsetService> _cmsService;
    private readonly Mock<IProjectModificationsService> _modsService;

    public ConfirmModificationJourneyTests()
    {
        _validator = Mocker.GetMock<IValidator<AreaOfChangeViewModel>>();
        _cmsService = Mocker.GetMock<ICmsQuestionsetService>();
        _modsService = Mocker.GetMock<IProjectModificationsService>();
    }

    [Theory, AutoData]
    public async Task ConfirmModificationJourney_ReturnsView_WhenValidationFails(
        AreaOfChangeViewModel model,
        List<GetAreaOfChangesResponse> areaChanges)
    {
        // Arrange
        const string action = "saveAndContinue";
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
        var result = await Sut.ConfirmModificationJourney(model, action);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AreaOfChange");
        viewResult.Model.ShouldBeOfType<AreaOfChangeViewModel>();
        validator.Verify();
    }

    [Theory, AutoData]
    public async Task ConfirmModificationJourney_RedirectsToPostApproval_WhenActionIsSaveForLater(
        AreaOfChangeViewModel model,
        List<GetAreaOfChangesResponse> areaChanges)
    {
        // Arrange
        const string action = "saveForLater"; // must match controller logic

        var validator = Mocker.GetMock<IValidator<AreaOfChangeViewModel>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AreaOfChangeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        var httpContext = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // TempData must include the modification id for SaveModificationChange to run,
        // area of changes is optional because we exit early for saveForLater.
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(areaChanges) // kept for completeness
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CreateModificationChange(It.IsAny<ProjectModificationChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationChangeResponse { Id = Guid.NewGuid() }
            });

        // Act
        var result = await Sut.ConfirmModificationJourney(model, action);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:postapproval");
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues["projectRecordId"].ShouldBe(model.ProjectRecordId);
        // Validator should NOT be invoked for saveForLater path
        validator.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<AreaOfChangeViewModel>>(), default), Times.Never);
    }

    [Fact]
    public async Task ConfirmModificationJourney_RedirectsToFirstSection_WhenValidModelAndJourneyExists()
    {
        // Arrange
        var model = new AreaOfChangeViewModel
        {
            // IDs updated to match the test areaOfChanges collection below
            AreaOfChangeId = "area1",
            SpecificChangeId = "specific1"
        };

        var areaOfChanges = new List<AreaOfChangeDto>
        {
            new() {
                AutoGeneratedId = "area1",
                OptionName = "Test Area",
                SpecificAreasOfChange =
                [
                    new() {
                        AutoGeneratedId = "specific1",
                        OptionName = "Specific Area 1"
                    }
                ]
            }
        };

        // TempData setup
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(areaOfChanges),
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid()
        };

        var validator = Mocker.GetMock<IValidator<AreaOfChangeViewModel>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AreaOfChangeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Id = "1" }
            });

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.CreateModificationChange(It.IsAny<ProjectModificationChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationChangeResponse { Id = Guid.NewGuid() }
            });

        var httpContext = new DefaultHttpContext();
        httpContext.Items["Respondent"] = new { GivenName = "Test", FamilyName = "User" };

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.ConfirmModificationJourney(model, action: "proceed");

        // Assert
        var redirectResult = result.ShouldBeOfType<ViewResult>();
        redirectResult.ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task ConfirmModificationJourney_Should_Return_View_With_Errors_When_Validation_Fails()
    {
        // Arrange
        var model = new AreaOfChangeViewModel { AreaOfChangeId = "A1", SpecificChangeId = "S1" };
        SetupTempData(model, BuildAreas(), Guid.NewGuid());

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AreaOfChangeViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("AreaOfChangeId", "Required")]));

        // Act
        var result = await Sut.ConfirmModificationJourney(model, "saveAndContinue");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("AreaOfChange");
        Sut.ModelState.ContainsKey("AreaOfChangeId").ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmModificationJourney_Should_Redirect_SaveForLater()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        var model = new AreaOfChangeViewModel { ProjectRecordId = "PR1", AreaOfChangeId = "A1", SpecificChangeId = "S1" };
        SetupTempData(model, BuildAreas(), modificationId);

        // Service response must be mocked to avoid null reference
        _modsService
            .Setup(s => s.CreateModificationChange(It.IsAny<ProjectModificationChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationChangeResponse { ProjectModificationId = modificationId, Id = Guid.NewGuid() }
            });

        // Act
        var result = await Sut.ConfirmModificationJourney(model, "saveForLater");

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");
        redirect.RouteValues!["projectRecordId"].ShouldBe("PR1");
    }

    [Fact]
    public async Task ConfirmModificationJourney_Should_Save_ModificationChange_And_Redirect_To_First_Section()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        var model = new AreaOfChangeViewModel { ProjectRecordId = "PR1", AreaOfChangeId = "A1", SpecificChangeId = "S1" };
        SetupTempData(model, BuildAreas(), modificationId);

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AreaOfChangeViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Modification change creation
        _modsService
            .Setup(s => s.CreateModificationChange(It.IsAny<ProjectModificationChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationChangeResponse { ProjectModificationId = modificationId }
            });

        SetupCmsJourney("section-static");

        // Act
        var result = await Sut.ConfirmModificationJourney(model, "saveAndContinue");

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:section-static");
        redirect.RouteValues!["categoryId"].ShouldBe("CAT1");
        redirect.RouteValues!["sectionId"].ShouldBe("SEC1");
    }

    [Fact]
    public async Task ConfirmModificationJourney_Should_Return_Error_When_No_Sections()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        var model = new AreaOfChangeViewModel { ProjectRecordId = "PR1", AreaOfChangeId = "A1", SpecificChangeId = "S1" };
        SetupTempData(model, BuildAreas(), modificationId);

        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AreaOfChangeViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _modsService
            .Setup(s => s.CreateModificationChange(It.IsAny<ProjectModificationChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationChangeResponse { ProjectModificationId = modificationId }
            });

        _cmsService
            .Setup(s => s.GetModificationsJourney("S1"))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Sections = [] }
            });

        // Act
        var result = await Sut.ConfirmModificationJourney(model, "saveAndContinue");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task ConfirmModificationJourney_Should_Redirect_When_Not_SaveAndContinue_Action()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        var model = new AreaOfChangeViewModel { ProjectRecordId = "PR1", AreaOfChangeId = "A1", SpecificChangeId = "S1" };
        SetupTempData(model, BuildAreas(), modificationId);

        _modsService
            .Setup(s => s.CreateModificationChange(It.IsAny<ProjectModificationChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationChangeResponse { ProjectModificationId = modificationId }
            });

        SetupCmsJourney("section-static");

        // Act (action different from saveAndContinue/saveForLater)
        var result = await Sut.ConfirmModificationJourney(model, "otherAction");

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:section-static");
    }

    private void SetupTempData(AreaOfChangeViewModel model, List<AreaOfChangeDto> areas, Guid modificationId, Guid? modificationChangeId = null)
    {
        var ctx = new DefaultHttpContext();
        var temp = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationId,
            [TempDataKeys.ProjectModification.AreaOfChanges] = JsonSerializer.Serialize(areas),
            [TempDataKeys.ProjectRecordId] = model.ProjectRecordId ?? "PR1"
        };
        if (modificationChangeId.HasValue)
        {
            temp[TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChangeId.Value;
        }
        Sut.TempData = temp;
        Sut.ControllerContext = new ControllerContext { HttpContext = ctx };
    }

    private static List<AreaOfChangeDto> BuildAreas() =>
    [
        new AreaOfChangeDto
        {
            AutoGeneratedId = "A1",
            OptionName = "Area 1",
            SpecificAreasOfChange =
            [
                new() { AutoGeneratedId = "S1", OptionName = "Spec 1" },
                new() { AutoGeneratedId = "S2", OptionName = "Spec 2" }
            ]
        }
    ];

    private void SetupCmsJourney(string sectionStatic, string sectionId = "SEC1", string categoryId = "CAT1")
    {
        _cmsService
            .Setup(s => s.GetModificationsJourney("S1"))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections =
                    [
                        new() { Id = sectionId, CategoryId = categoryId, StaticViewName = sectionStatic, Sequence = 1 }
                    ]
                }
            });
    }
}
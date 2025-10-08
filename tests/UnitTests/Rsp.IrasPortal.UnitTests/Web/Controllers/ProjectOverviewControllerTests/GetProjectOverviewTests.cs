using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers.ProjectOverview;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectOverviewControllerTests;

public class GetProjectOverviewTests : TestServiceBase<ProjectOverviewController>
{
    [Fact]
    public async Task GetProjectOverview_ReturnsErrorView_WhenProjectRecordServiceFails()
    {
        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();
        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.InternalServerError });

        // Ensure HttpContext/Request is set up
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.GetProjectOverview("rec-1");

        // Assert
        var redirectToRouteResult = result.ShouldBeOfType<StatusCodeResult>();
        redirectToRouteResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task GetProjectOverview_ReturnsErrorView_WhenProjectRecordIsNull()
    {
        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = null });

        // Ensure HttpContext/Request is set up
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.GetProjectOverview("rec-1");

        // Assert
        var redirectToRouteResult = result.ShouldBeOfType<StatusCodeResult>();
        redirectToRouteResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetProjectOverview_ReturnsErrorView_WhenRespondentAnswersServiceFails()
    {
        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();
        var respondentService = Mocker.GetMock<IRespondentService>();

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse { Id = "rec-1", IrasId = 1 } });

        respondentService
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecrod))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.InternalServerError });

        // Ensure HttpContext/Request is set up
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.GetProjectOverview("rec-1");

        // Assert
        var redirectToRouteResult = result.ShouldBeOfType<StatusCodeResult>();
        redirectToRouteResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task GetProjectOverview_ReturnsErrorView_WhenRespondentAnswersAreNull()
    {
        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();
        var respondentService = Mocker.GetMock<IRespondentService>();

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse { Id = "rec-1", IrasId = 1 } });

        respondentService
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecrod))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = null });

        // Ensure HttpContext/Request is set up
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.GetProjectOverview("rec-1");

        // Assert
        var redirectToRouteResult = result.ShouldBeOfType<StatusCodeResult>();
        redirectToRouteResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetProjectOverview_PopulatesTempDataAndReturnsView_WhenDataIsPresent()
    {
        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();
        var respondentService = Mocker.GetMock<IRespondentService>();

        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse { Id = "rec-1", IrasId = 1 } });

        respondentService
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecrod))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        // Initialize TempData for the controller
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Ensure HttpContext/Request is set up
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.GetProjectOverview("rec-1");

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();

        var model = okResult.Value.ShouldBeOfType<ProjectOverviewModel>();
        model.ProjectTitle.ShouldBe("Project X");
        model.ProjectRecordId.ShouldBe("rec-1");
        model.CategoryId.ShouldBe(QuestionCategories.ProjectRecrod);
        model.ProjectPlannedEndDate.ShouldNotBeNullOrEmpty();

        Sut.TempData[TempDataKeys.IrasId].ShouldBe(1);
        Sut.TempData[TempDataKeys.ProjectRecordId].ShouldBe("rec-1");
        Sut.TempData[TempDataKeys.ShortProjectTitle].ShouldBe("Project X");
    }

    [Fact]
    public async Task GetProjectOverview_SetsProjectPlannedEndDateTempData_WhenEndDateIsValid()
    {
        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();
        var respondentService = Mocker.GetMock<IRespondentService>();
        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse { Id = "rec-1", IrasId = 1 } });

        respondentService
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecrod))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        // Initialize TempData for the controller
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Ensure HttpContext/Request is set up
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        await Sut.GetProjectOverview("rec-1");

        // Assert
        Sut.TempData[TempDataKeys.PlannedProjectEndDate].ShouldBe("01 January 2025");
    }

    [Fact]
    public async Task GetProjectOverview_ReturnsFullyPopulatedModel_WhenDataIsPresent()
    {
        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();
        var respondentService = Mocker.GetMock<IRespondentService>();

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
            new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" },
            new() { QuestionId = QuestionIds.ParticipatingNations, Answers = new List<string> { QuestionAnswersOptionsIds.England, QuestionAnswersOptionsIds.Scotland } },
            new() { QuestionId = QuestionIds.NhsOrHscOrganisations, SelectedOption = QuestionAnswersOptionsIds.Yes },
            new() { QuestionId = QuestionIds.LeadNation, SelectedOption = QuestionAnswersOptionsIds.Wales },
            new() { QuestionId = QuestionIds.ChiefInvestigator, AnswerText = "Dr. Jane Doe" },
            new() { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = "University of Example" },
            new() { QuestionId = QuestionIds.SponsorContact, AnswerText = "jane.doe@example.com" }
        };

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse { Id = "rec-1", IrasId = 1, Status = ModificationStatus.ModificationRecordStarted }
            });

        respondentService
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecrod))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = answers
            });

        // Initialize TempData for the controller
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Ensure HttpContext/Request is set up
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.GetProjectOverview("rec-1");

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();

        var model = okResult.Value.ShouldBeOfType<ProjectOverviewModel>();

        model.ProjectTitle.ShouldBe("Project X");
        model.ProjectRecordId.ShouldBe("rec-1");
        model.CategoryId.ShouldBe(QuestionCategories.ProjectRecrod);
        model.ProjectPlannedEndDate.ShouldBe("01 January 2025");
        model.Status.ShouldBe(ModificationStatus.ModificationRecordStarted);
        model.IrasId.ShouldBe(1);
        model.ParticipatingNations.ShouldBe(new List<string> { "England", "Scotland" });
        model.NhsOrHscOrganisations.ShouldBe("Yes");
        model.LeadNation.ShouldBe("Wales");
        model.ChiefInvestigator.ShouldBe("Dr. Jane Doe");
        model.PrimarySponsorOrganisation.ShouldBe("University of Example");
        model.SponsorContact.ShouldBe("jane.doe@example.com");
    }
}
using AutoFixture;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
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
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecord))
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
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = null });

        // Ensure HttpContext/Request is set up
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.GetProjectOverview("rec-1");

        // Assert
        var redirectToRouteResult = result.ShouldBeOfType<StatusCodeResult>();
        redirectToRouteResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Theory, RecursionSafeAutoData]
    public async Task GetProjectOverview_PopulatesTempDataAndReturnsView_WhenDataIsPresent(CmsQuestionSetResponse cmsResponse)
    {
        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();
        var respondentService = Mocker.GetMock<IRespondentService>();
        var cmsService = Mocker.GetMock<ICmsQuestionsetService>();

        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse { Id = "rec-1", IrasId = 1 } });

        respondentService
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        cmsService
            .Setup(s => s.GetQuestionSet(null, null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = cmsResponse
            });

        applicationService
            .Setup(s => s.GetProjectRecordAuditTrail("rec-1"))
            .ReturnsAsync(new ServiceResponse<ProjectRecordAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectRecordAuditTrailResponse
                {
                    Items = []
                }
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
        model.CategoryId.ShouldBe(QuestionCategories.ProjectRecord);
        model.ProjectPlannedEndDate.ShouldNotBeNullOrEmpty();

        Sut.TempData[TempDataKeys.IrasId].ShouldBe(1);
        Sut.TempData[TempDataKeys.ProjectRecordId].ShouldBe("rec-1");
        Sut.TempData[TempDataKeys.ShortProjectTitle].ShouldBe("Project X");
    }

    [Theory, RecursionSafeAutoData]
    public async Task GetProjectOverview_SetsProjectPlannedEndDateTempData_WhenEndDateIsValid(CmsQuestionSetResponse cmsResponse)
    {
        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();
        var respondentService = Mocker.GetMock<IRespondentService>();
        var cmsService = Mocker.GetMock<ICmsQuestionsetService>();

        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse { Id = "rec-1", IrasId = 1 } });

        respondentService
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        cmsService
            .Setup(s => s.GetQuestionSet(null, null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = cmsResponse
            });

        applicationService
            .Setup(s => s.GetProjectRecordAuditTrail("rec-1"))
            .ReturnsAsync(new ServiceResponse<ProjectRecordAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectRecordAuditTrailResponse
                {
                    Items = []
                }
            });

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
        var cmsService = Mocker.GetMock<ICmsQuestionsetService>();
        var rtsService = Mocker.GetMock<IRtsService>();

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
            new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" },
            new() { QuestionId = QuestionIds.ParticipatingNations, Answers = new List<string> { QuestionAnswersOptionsIds.England, QuestionAnswersOptionsIds.Scotland } },
            new() { QuestionId = QuestionIds.NhsOrHscOrganisations, SelectedOption = QuestionAnswersOptionsIds.Yes },
            new() { QuestionId = QuestionIds.LeadNation, SelectedOption = QuestionAnswersOptionsIds.Wales },
            new() { QuestionId = QuestionIds.FirstName, AnswerText = "Dr. Jane Doe" },
            new() { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = "1" },
            new() { QuestionId = QuestionIds.Email, AnswerText = "jane.doe@example.com" }
        };

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse { Id = "rec-1", IrasId = 1, Status = ModificationStatus.InDraft }
            });

        respondentService
            .Setup(s => s.GetRespondentAnswers("rec-1", QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = answers
            });

        applicationService
            .Setup(s => s.GetProjectRecordAuditTrail("rec-1"))
            .ReturnsAsync(new ServiceResponse<ProjectRecordAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectRecordAuditTrailResponse
                {
                    Items = []
                }
            });

        // Add primary sponsor organisation question to cms response
        var question = new QuestionModel
        {
            Id = QuestionIds.PrimarySponsorOrganisation,
            AnswerDataType = "rts:org_lookup",
            QuestionFormat = "rts:org_lookup",
            Answers = new List<AnswerModel>
            {
            new AnswerModel
                {
                    Id = QuestionIds.PrimarySponsorOrganisation,
                    OptionName = "1"
                }
            }
        };

        CmsQuestionSetResponse cmsResponse = new CmsQuestionSetResponse();

        var section = new SectionModel
        {
            Id = "123",
            Questions = new List<QuestionModel>()
        };

        section.Questions.Add(question);
        cmsResponse.Sections.Add(section);

        cmsService
            .Setup(s => s.GetQuestionSet(null, null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = cmsResponse
            });

        rtsService
                .Setup(s => s.GetOrganisation("1"))
                .ReturnsAsync(new ServiceResponse<OrganisationDto>
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new OrganisationDto { Name = "Test Organisation" }
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
        model.CategoryId.ShouldBe(QuestionCategories.ProjectRecord);
        model.ProjectPlannedEndDate.ShouldBe("01 January 2025");
        model.Status.ShouldBe(ModificationStatus.InDraft);
        model.IrasId.ShouldBe(1);
        model.OrganisationName.ShouldBe("Test Organisation");
        model.SectionGroupQuestions.ShouldNotBeNull();
        model.SectionGroupQuestions.ShouldBeOfType<List<SectionGroupWithQuestionsViewModel>>();
        model.SectionGroupQuestions.ShouldBeEmpty();
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RecursionSafeAutoDataAttribute : AutoDataAttribute
    {
        public RecursionSafeAutoDataAttribute()
            : base(() =>
            {
                var fixture = new Fixture();
                fixture.Behaviors
                    .OfType<ThrowingRecursionBehavior>()
                    .ToList()
                    .ForEach(b => fixture.Behaviors.Remove(b));

                fixture.Behaviors.Add(new OmitOnRecursionBehavior());

                return fixture;
            })
        {
        }
    }
}
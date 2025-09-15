using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers.ProjectOverview;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectOverviewControllerTests;

public class ProjectOverviewTests : TestServiceBase<ProjectOverviewController>
{
    private const string DefaultProjectRecordId = "123";

    private TempDataDictionary CreateTempData(Mock<ITempDataProvider> tempDataProvider, HttpContext httpContext)
    {
        return new TempDataDictionary(httpContext, tempDataProvider.Object);
    }

    private void SetupProjectRecord(string projectRecordId)
    {
        var applicationService = Mocker.GetMock<IApplicationsService>();
        applicationService
            .Setup(s => s.GetProjectRecord(projectRecordId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse
                {
                    Id = projectRecordId,
                    IrasId = 1,
                    Status = "Draft"
                }
            });
    }

    private void SetupRespondentAnswers(string projectRecordId, IEnumerable<RespondentAnswerDto> answers)
    {
        var respondentService = Mocker.GetMock<IRespondentService>();
        respondentService
            .Setup(s => s.GetRespondentAnswers(projectRecordId, QuestionCategories.ProjectRecrod))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = answers
            });
    }

    private void SetupControllerContext(HttpContext httpContext, ITempDataDictionary tempData)
    {
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        Sut.TempData = tempData;
    }

    [Fact]
    public async Task ProjectDetails_UsesTempData_AndReturnsViewResult()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        tempData[TempDataKeys.ShortProjectTitle] = "Test Project";
        tempData[TempDataKeys.CategoryId] = QuestionCategories.ProjectRecrod;
        tempData[TempDataKeys.ProjectRecordId] = DefaultProjectRecordId;

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Test Project" },
            new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
        };

        SetupProjectRecord(DefaultProjectRecordId);
        SetupRespondentAnswers(DefaultProjectRecordId, answers);
        SetupControllerContext(httpContext, tempData);

        // Act
        var result = await Sut.ProjectDetails(DefaultProjectRecordId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectOverviewModel>();

        model.ProjectTitle.ShouldBe("Test Project");
        model.CategoryId.ShouldBe(QuestionCategories.ProjectRecrod);
        model.ProjectRecordId.ShouldBe(DefaultProjectRecordId);
    }

    [Fact]
    public async Task ProjectDetails_RemovesModificationRelatedTempDataKeys()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = "mod-1";
        tempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "ident-1";
        tempData[TempDataKeys.ProjectModification.ProjectModificationChangeId] = "chg-1";
        tempData[TempDataKeys.ProjectModification.ProjectModificationSpecificArea] = "area-1";
        tempData[$"{TempDataKeys.ProjectModification.Questionnaire}_abc"] = "questionnaire-data"; // dynamic key to test removal by prefix

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
            new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
        };

        SetupProjectRecord(DefaultProjectRecordId);
        SetupRespondentAnswers(DefaultProjectRecordId, answers);
        SetupControllerContext(httpContext, tempData);

        // Act
        await Sut.ProjectDetails(DefaultProjectRecordId);

        // Assert
        tempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationId).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationIdentifier).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationChangeId).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationSpecificArea).ShouldBeFalse();
        tempData.Keys.Any(k => k.StartsWith(TempDataKeys.ProjectModification.Questionnaire)).ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectDetails_SetsNotificationBanner_WhenMarkerPresent()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid();
        tempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker] = Guid.NewGuid();

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" }
        };

        SetupProjectRecord(DefaultProjectRecordId);
        SetupRespondentAnswers(DefaultProjectRecordId, answers);
        SetupControllerContext(httpContext, tempData);

        // Act
        await Sut.ProjectDetails(DefaultProjectRecordId);

        // Assert
        tempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        tempData[TempDataKeys.ProjectOverview].ShouldBe(true);
    }

    [Fact]
    public async Task ProjectDetails_SetsProjectOverviewTempDataKey_WhenValidDataProvided()
    {
        // Arrange
        var projectRecordId = "rec-1";
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

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

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);

        // Act
        var result = await Sut.ProjectDetails(projectRecordId);

        // Assert
        tempData[TempDataKeys.ProjectOverview].ShouldBe(true);
    }

    [Fact]
    public async Task ProjectDetails_SetsProjectOverviewProblemDetails()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        SetupProjectRecord(DefaultProjectRecordId);

        var respondentService = Mocker.GetMock<IRespondentService>();
        respondentService
            .Setup(s => s.GetRespondentAnswers(DefaultProjectRecordId, QuestionCategories.ProjectRecrod))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        SetupControllerContext(httpContext, tempData);

        // Act
        var result = await Sut.ProjectDetails(DefaultProjectRecordId);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task KeyProjectRoles_ReturnsViewResult_WithKeyProjectRoleData()
    {
        // Arrange
        var projectRecordId = "123";
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ChiefInvestigator, AnswerText = "Dr. Jane Doe" },
            new() { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = "University of Example" },
            new() { QuestionId = QuestionIds.SponsorContact, AnswerText = "jane.doe@example.com" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);

        // Act
        var result = await Sut.KeyProjectRoles(projectRecordId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectOverviewModel>();

        model.ChiefInvestigator.ShouldBe("Dr. Jane Doe");
        model.PrimarySponsorOrganisation.ShouldBe("University of Example");
        model.SponsorContact.ShouldBe("jane.doe@example.com");
    }

    [Fact]
    public async Task ResearchLocations_ReturnsViewResult_WithResearchLocationData()
    {
        // Arrange
        var projectRecordId = "123";
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ParticipatingNations, Answers = new List<string> { QuestionAnswersOptionsIds.England, QuestionAnswersOptionsIds.Scotland } },
            new() { QuestionId = QuestionIds.NhsOrHscOrganisations, SelectedOption = QuestionAnswersOptionsIds.Yes },
            new() { QuestionId = QuestionIds.LeadNation, SelectedOption = QuestionAnswersOptionsIds.Wales }
        };

        var answerOptions = new Dictionary<string, string>
        {
            { QuestionAnswersOptionsIds.England, "England" },
            { QuestionAnswersOptionsIds.Scotland, "Scotland" },
            { QuestionAnswersOptionsIds.Wales, "Wales" },
            { QuestionAnswersOptionsIds.Yes, "Yes" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);

        // Act
        var result = await Sut.ResearchLocations(projectRecordId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectOverviewModel>();

        model.ParticipatingNations.ShouldBe(new List<string> { "England", "Scotland" });
        model.NhsOrHscOrganisations.ShouldBe("Yes");
        model.LeadNation.ShouldBe("Wales");
    }

    [Fact]
    public async Task PostApproval_ReturnsViewResult_WithExpectedModel()
    {
        // Arrange
        var projectRecordId = "123";
        var pageNumber = 1;
        var pageSize = 20;
        var sortField = nameof(ModificationsModel.CreatedAt);
        var sortDirection = SortDirections.Ascending;

        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);

        var modifications = new List<ModificationsDto>
        {
            new() { ModificationId = "mod1", ModificationType = "TypeA" },
            new() { ModificationId = "mod2", ModificationType = "TypeB" }
        };

        var modificationsResponse = new GetModificationsResponse
        {
            Modifications = modifications,
            TotalCount = modifications.Count
        };

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationsResponse
        };

        var projectModificationsService = Mocker.GetMock<IProjectModificationsService>();

        projectModificationsService
                .Setup(s => s.GetModificationsForProject(projectRecordId, It.IsAny<ModificationSearchRequest>(), pageNumber, pageSize, sortField, sortDirection))
                .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.PostApproval(projectRecordId, pageNumber, pageSize, sortField, sortDirection);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PostApprovalViewModel>();

        model.ProjectOverviewModel.ShouldNotBeNull();
        model.Modifications.Count().ShouldBe(modifications.Count);
        model.Pagination.ShouldNotBeNull();
        model.Pagination.PageNumber.ShouldBe(pageNumber);
        model.Pagination.PageSize.ShouldBe(pageSize);
        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);
    }

    [Fact]
    public async Task PostApproval_MapsModifications_WithDraftStatus()
    {
        // Arrange
        var projectRecordId = "456";
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project Y" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);

        var modifications = new List<ModificationsDto>
        {
            new() { ModificationId = "m1", ModificationType = "Type1" }
        };

        var modificationsResponse = new GetModificationsResponse
        {
            Modifications = modifications,
            TotalCount = 1
        };

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationsResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsForProject(projectRecordId, It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.PostApproval(projectRecordId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PostApprovalViewModel>();
        var mod = model.Modifications.Single();
        mod.ModificationId.ShouldBe("m1");
        mod.ModificationType.ShouldBe("Type1");
        mod.Status.ShouldBe("Draft");
        mod.ReviewType.ShouldBeNull();
        mod.Category.ShouldBeNull();
        mod.DateSubmitted.ShouldBeNull();
    }

    [Fact]
    public async Task PostApproval_ReturnsEmptyModifications_WhenServiceReturnsNullContent()
    {
        // Arrange
        var projectRecordId = "789";
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project Z" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = null
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsForProject(projectRecordId, It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.PostApproval(projectRecordId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PostApprovalViewModel>();
        model.Modifications.ShouldBeEmpty();
        model.Pagination.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task PostApproval_PropagatesError_WhenGetProjectOverviewFails()
    {
        // Arrange
        var projectRecordId = "err-1";
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);
        SetupControllerContext(httpContext, tempData);

        // Cause application service to fail inside GetProjectOverview
        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(projectRecordId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.PostApproval(projectRecordId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}
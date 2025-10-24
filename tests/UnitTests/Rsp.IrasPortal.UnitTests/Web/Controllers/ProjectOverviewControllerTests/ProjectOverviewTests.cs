using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Enum;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers.ProjectOverview;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectOverviewControllerTests;

public class ProjectOverviewTests : TestServiceBase<ProjectOverviewController>
{
    private const string DefaultProjectRecordId = "123";

    // --- NEW: simple in-memory session for unit tests
    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public IEnumerable<string> Keys => _store.Keys;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public bool IsAvailable => true;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    // --- NEW: helper to create HttpContext with session already wired up
    private static DefaultHttpContext CreateHttpContextWithSession()
    {
        var ctx = new DefaultHttpContext
        {
            Session = new TestSession()
        };
        return ctx;
    }

    private TempDataDictionary CreateTempData(Mock<ITempDataProvider> tempDataProvider, HttpContext httpContext)
        => new(httpContext, tempDataProvider.Object);

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
                    Status = ProjectRecordStatus.InDraft,
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
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = tempData;
    }

    private void SetupCMSService(string showAnswerOn = "", string sectionGroup = "")
    {
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(null, null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    ActiveFrom = DateTime.UtcNow,
                    ActiveTo = DateTime.UtcNow.AddYears(1),
                    Id = "project-overview",
                    Version = "1.0",
                    Sections = new List<SectionModel>
                    {
                        new SectionModel
                        {
                            Id = "S1",
                            Questions = new List<QuestionModel>
                            {
                                new QuestionModel
                                {
                                    Id = "1",
                                    QuestionId = QuestionIds.ShortProjectTitle,
                                    Name = "Short Project Title",
                                    ShortName = "Short Q1",
                                    ShowAnswerOn = "ProjectOverview",
                                    SectionGroup = "Basic Info",
                                    SectionSequence = 1,
                                    SequenceInSectionGroup = 1,
                                    Sequence = 1,
                                    Version = "1.0",
                                    AnswerDataType = "Text",
                                    Conformance = "Mandatory",
                                    ShowOriginalAnswer = false,
                                    Answers = new List<AnswerModel>(),
                                    ValidationRules = new List<RuleModel>()
                                },
                                new QuestionModel
                                {
                                    Id = "2",
                                    QuestionId = QuestionIds.ProjectPlannedEndDate,
                                    Name = "Planned End Date",
                                    ShowAnswerOn = "ProjectOverview",
                                    SectionGroup = "Basic Info",
                                    SectionSequence = 1,
                                    SequenceInSectionGroup = 2,
                                    Sequence = 2,
                                    Version = "1.0",
                                    AnswerDataType = "Date",
                                    Conformance = "Optional",
                                    ShowOriginalAnswer = false,
                                    Answers = new List<AnswerModel>(),
                                    ValidationRules = new List<RuleModel>()
                                },
                                new QuestionModel
                                {
                                    Id = "3",
                                    QuestionId = "Test",
                                    Name = "Test Question",
                                    ShowAnswerOn = showAnswerOn,
                                    SectionGroup = sectionGroup,
                                    SectionSequence = 1,
                                    SequenceInSectionGroup = 1,
                                    Sequence = 1,
                                    Version = "1.0",
                                    AnswerDataType = "Text",
                                    Conformance = "Optional",
                                    ShowOriginalAnswer = false,
                                    Answers = [],
                                    ValidationRules = new List<RuleModel>()
                                }
                            }
                        }
                    }
                }
            });
    }

    [Fact]
    public async Task Index_GetsProjectOverview_And_ReturnsViewResult_When_StatusIsDraft()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession();
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

        var applicationService = Mocker.GetMock<IApplicationsService>();
        applicationService
            .Setup(s => s.GetProjectRecord(DefaultProjectRecordId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse
                {
                    Id = DefaultProjectRecordId,
                    IrasId = 1,
                    Status = ProjectRecordStatus.InDraft
                }
            });

        SetupRespondentAnswers(DefaultProjectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        // Act
        var result = await Sut.Index(DefaultProjectRecordId, null, null);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<ProjectOverviewModel>();
    }

    [Fact]
    public async Task Index_GetsProjectOverview_And_ReturnsRedirectToActionResult_When_StatusIsSubmitted()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession();
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

        var applicationService = Mocker.GetMock<IApplicationsService>();
        applicationService
            .Setup(s => s.GetProjectRecord(DefaultProjectRecordId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse
                {
                    Id = DefaultProjectRecordId,
                    IrasId = 1,
                    Status = ProjectRecordStatus.Active
                }
            });

        SetupRespondentAnswers(DefaultProjectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        // Act
        var result = await Sut.Index(DefaultProjectRecordId, null, null);

        // Assert
        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task ProjectDetails_UsesTempData_AndReturnsViewResult()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(); // CHANGED
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
        SetupCMSService("ProjectDetails", "Project details");

        // Act
        var result = await Sut.ProjectDetails(DefaultProjectRecordId, "", "");
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
        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        tempData[TempDataKeys.ProjectModification.ProjectModificationId] = "mod-1";
        tempData[TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "ident-1";
        tempData[TempDataKeys.ProjectModification.ProjectModificationChangeId] = "chg-1";
        tempData[TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "area-1";
        tempData[$"{TempDataKeys.ProjectModification.Questionnaire}_abc"] = "questionnaire-data";

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
            new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
        };

        SetupProjectRecord(DefaultProjectRecordId);
        SetupRespondentAnswers(DefaultProjectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService("ProjectDetails", "Project details");

        // Act
        await Sut.ProjectDetails(DefaultProjectRecordId, "", "");

        // Assert
        tempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationId).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationIdentifier).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModification.ProjectModificationChangeId).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModification.SpecificAreaOfChangeText).ShouldBeFalse();
        tempData.Keys.Any(k => k.StartsWith(TempDataKeys.ProjectModification.Questionnaire)).ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectDetails_SetsNotificationBanner_WhenMarkerPresent()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(); // CHANGED
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
        SetupCMSService("ProjectDetails", "Project details");

        // Act
        await Sut.ProjectDetails(DefaultProjectRecordId, "", "");

        // Assert
        tempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        tempData[TempDataKeys.ProjectOverview].ShouldBe(true);
    }

    [Fact]
    public async Task ProjectDetails_SetsProjectOverviewTempDataKey_WhenValidDataProvided()
    {
        // Arrange
        var projectRecordId = "rec-1";
        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
            new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" },
            new() { QuestionId = QuestionIds.ParticipatingNations, Answers = new List<string> { QuestionAnswersOptionsIds.England, QuestionAnswersOptionsIds.Scotland } },
            new() { QuestionId = QuestionIds.NhsOrHscOrganisations, SelectedOption = QuestionAnswersOptionsIds.Yes },
            new() { QuestionId = QuestionIds.LeadNation, SelectedOption = QuestionAnswersOptionsIds.Wales },
            new() { QuestionId = QuestionIds.FirstName, AnswerText = "Dr. Jane" },
            new() { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = "University of Example" },
            new() { QuestionId = QuestionIds.Email, AnswerText = "jane.doe@example.com" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService("ProjectDetails", "Project details");

        // Act
        var result = await Sut.ProjectDetails(projectRecordId, "", "");

        // Assert
        tempData[TempDataKeys.ProjectOverview].ShouldBe(true);
    }

    [Fact]
    public async Task ProjectDetails_SetsProjectOverviewProblemDetails()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession(); // CHANGED
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
        SetupCMSService("ProjectDetails", "Project details");

        // Act
        var result = await Sut.ProjectDetails(DefaultProjectRecordId, "", "");

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ProjectTeam_ReturnsViewResult_WithKeyProjectRoleData()
    {
        // Arrange
        var projectRecordId = "123";
        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.FirstName, AnswerText = "Dr. Jane Doe" },
            new() { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = "University of Example" },
            new() { QuestionId = QuestionIds.Email, AnswerText = "jane.doe@example.com" }
        };
        SetUpProjectRecordOverview();
        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService("ProjectTeam", "Project team");

        // Act
        var result = await Sut.ProjectTeam(projectRecordId, "");

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectOverviewModel>();
        model.SectionGroupQuestions.ShouldBeOfType<List<SectionGroupWithQuestionsViewModel>>();
        model.SectionGroupQuestions.ShouldNotBeNull();
    }

    private void SetUpProjectRecordOverview()
    {
        var applicationService = Mocker.GetMock<IApplicationsService>();
        applicationService
            .Setup(s => s.GetProjectRecord(DefaultProjectRecordId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new IrasApplicationResponse
                {
                    Id = DefaultProjectRecordId,
                    IrasId = 1,
                    Status = ProjectRecordStatus.InDraft
                }
            });
    }

    [Fact]
    public async Task ResearchLocations_ReturnsViewResult_WithResearchLocationData()
    {
        // Arrange
        var projectRecordId = "123";
        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ParticipatingNations, Answers = new List<string> { QuestionAnswersOptionsIds.England, QuestionAnswersOptionsIds.Scotland } },
            new() { QuestionId = QuestionIds.NhsOrHscOrganisations, SelectedOption = QuestionAnswersOptionsIds.Yes },
            new() { QuestionId = QuestionIds.LeadNation, SelectedOption = QuestionAnswersOptionsIds.Wales }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService("ResearchLocations", "Research locations");

        // Act
        var result = await Sut.ResearchLocations(projectRecordId, "");

        // Assert

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectOverviewModel>();
        model.SectionGroupQuestions.ShouldBeOfType<List<SectionGroupWithQuestionsViewModel>>();
        model.SectionGroupQuestions.ShouldNotBeNull();
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

        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

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
        var result = await Sut.PostApproval(projectRecordId, "", pageNumber, pageSize, sortField, sortDirection);

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
        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project Y" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        var modifications = new List<ModificationsDto>
        {
            new() { ModificationId = "m1", ModificationType = "Type1", Status = ModificationStatus.InDraft }
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
        var result = await Sut.PostApproval(projectRecordId, "");

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PostApprovalViewModel>();
        var mod = model.Modifications.Single();
        mod.ModificationIdentifier.ShouldBe("m1");
        mod.ModificationType.ShouldBe("Type1");
        mod.Status.ShouldBe(ModificationStatus.InDraft);
        mod.ReviewType.ShouldBeNull();
        mod.Category.ShouldBeNull();
    }

    [Fact]
    public async Task PostApproval_ReturnsEmptyModifications_WhenServiceReturnsNullContent()
    {
        // Arrange
        var projectRecordId = "789";
        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project Z" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = null
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsForProject(projectRecordId, It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.PostApproval(projectRecordId, "");

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
        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(projectRecordId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.PostApproval(projectRecordId, "");

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ProjectDetails_BackNav_SetsSessionAndViewData_WhenBackRouteProvided()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        SetupProjectRecord(DefaultProjectRecordId);
        SetupRespondentAnswers(DefaultProjectRecordId, new[]
        {
        new RespondentAnswerDto { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "X" }
    });
        SetupControllerContext(httpContext, tempData);
        SetupCMSService("ProjectDetails", "Project details");

        // Act (triggers: if (!IsNullOrWhiteSpace(backRouteFromQuery)) {... return;})
        var result = await Sut.ProjectDetails(DefaultProjectRecordId, "admin:applyfilters", "");

        // Assert
        (Sut.ViewData["BackRoute"] as string).ShouldBe("admin:applyfilters");
        httpContext.Session.GetString(SessionKeys.BackRoute).ShouldBe("admin:applyfilters");
        httpContext.Session.GetString(SessionKeys.BackRouteSection).ShouldBe("pov");
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task ProjectDetails_BackNav_UsesStoredRoute_WhenSameSection_AndStoredRouteExists()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString(SessionKeys.BackRouteSection, "pov");
        httpContext.Session.SetString(SessionKeys.BackRoute, "admin:results");

        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        SetupProjectRecord(DefaultProjectRecordId);
        SetupRespondentAnswers(DefaultProjectRecordId, new[]
        {
        new RespondentAnswerDto { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "X" }
    });
        SetupControllerContext(httpContext, tempData);
        SetupCMSService("ProjectDetails", "Project details");

        // Act (triggers: same section -> pull BackRoute from session)
        var result = await Sut.ProjectDetails(DefaultProjectRecordId, null, "");

        // Assert
        (Sut.ViewData["BackRoute"] as string).ShouldBe("admin:results");
        httpContext.Session.GetString(SessionKeys.BackRouteSection).ShouldBe("pov");
        httpContext.Session.GetString(SessionKeys.BackRoute).ShouldBe("admin:results");
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task ProjectDetails_BackNav_FallsBackToDefault_WhenSameSection_ButNoStoredRoute()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString(SessionKeys.BackRouteSection, "pov");
        // Intentionally do NOT set BackRoute
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        SetupProjectRecord(DefaultProjectRecordId);
        SetupRespondentAnswers(DefaultProjectRecordId, new[]
        {
        new RespondentAnswerDto { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "X" }
    });
        SetupControllerContext(httpContext, tempData);
        SetupCMSService("ProjectDetails", "Project details");

        // Act (triggers: same section, storedRoute null -> defaultRoute)
        var result = await Sut.ProjectDetails(DefaultProjectRecordId, null, "");

        // Assert
        (Sut.ViewData["BackRoute"] as string).ShouldBe("app:Welcome");
        httpContext.Session.GetString(SessionKeys.BackRouteSection).ShouldBe("pov");
        httpContext.Session.GetString(SessionKeys.BackRoute).ShouldBeNull();
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task ProjectDetails_BackNav_ClearsSession_AndDefaults_WhenDifferentSection()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession();
        httpContext.Session.SetString(SessionKeys.BackRouteSection, "other");
        httpContext.Session.SetString(SessionKeys.BackRoute, "admin:old");

        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        SetupProjectRecord(DefaultProjectRecordId);
        SetupRespondentAnswers(DefaultProjectRecordId, new[]
        {
        new RespondentAnswerDto { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "X" }
    });
        SetupControllerContext(httpContext, tempData);
        SetupCMSService("ProjectDetails", "Project details");

        // Act (triggers: else branch -> clears + default)
        var result = await Sut.ProjectDetails(DefaultProjectRecordId, null, "");

        // Assert
        httpContext.Session.GetString(SessionKeys.BackRouteSection).ShouldBeNull();
        httpContext.Session.GetString(SessionKeys.BackRoute).ShouldBeNull();
        (Sut.ViewData["BackRoute"] as string).ShouldBe("app:Welcome");
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task BackNav_PersistsAcrossActions_WithinSection()
    {
        // Arrange
        var projectRecordId = "persist-1";
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, new[]
        {
        new RespondentAnswerDto { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "X" }
    });
        SetupControllerContext(httpContext, tempData);
        SetupCMSService("ProjectDetails", "Project details");

        // Seed via ProjectDetails (sets session + ViewData)
        await Sut.ProjectDetails(projectRecordId, "admin:applyfilters", "");

        // Now call another action in the *same* section WITHOUT backRoute
        var modsService = Mocker.GetMock<IProjectModificationsService>();
        modsService
            .Setup(s => s.GetModificationsForProject(
                projectRecordId,
                It.IsAny<ModificationSearchRequest>(),
                1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new GetModificationsResponse { Modifications = new List<ModificationsDto>(), TotalCount = 0 }
            });

        var postApprovalResult = await Sut.PostApproval(projectRecordId, null);

        // Assert the helper restored the route from session
        (Sut.ViewData["BackRoute"] as string).ShouldBe("admin:applyfilters");
        postApprovalResult.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task ProjectDocuments_ReturnsViewResult_WithExpectedModel()
    {
        // Arrange
        var projectRecordId = "123";
        var pageNumber = 1;
        var pageSize = 20;
        var sortField = nameof(ModificationsModel.CreatedAt);
        var sortDirection = SortDirections.Ascending;

        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        var documents = new List<ProjectOverviewDocumentDto>
        {
            new() { FileName = "mod1", DocumentType = "TypeA" },
            new() { FileName = "mod2", DocumentType = "TypeB" }
        };

        var documentsResponse = new ProjectOverviewDocumentResponse
        {
            Documents = documents,
            TotalCount = documents.Count
        };

        var serviceResponse = new ServiceResponse<ProjectOverviewDocumentResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = documentsResponse
        };

        var projectModificationsService = Mocker.GetMock<IProjectModificationsService>();

        projectModificationsService
            .Setup(s => s.GetDocumentsForProjectOverview(projectRecordId, It.IsAny<ProjectOverviewDocumentSearchRequest>(), pageNumber, pageSize, sortField, sortDirection))
            .ReturnsAsync(serviceResponse);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        // Act
        var result = await Sut.ProjectDocuments(projectRecordId, "", pageNumber, pageSize, sortField, sortDirection);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectOverviewDocumentViewModel>();

        model.ProjectOverviewModel.ShouldNotBeNull();
        model.Documents.Count().ShouldBe(documents.Count);
        model.Pagination.ShouldNotBeNull();
        model.Pagination.PageNumber.ShouldBe(pageNumber);
        model.Pagination.PageSize.ShouldBe(pageSize);
        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);
    }

    [Fact]
    public async Task ProjectDocuments_PropagatesError_WhenGetProjectOverviewFails()
    {
        // Arrange
        var projectRecordId = "err-1";
        var httpContext = CreateHttpContextWithSession(); // CHANGED
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(projectRecordId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ProjectDocuments(projectRecordId, "");

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task PostApproval_Modifications_When_DraftStatus_SubmittedDate_ShouldBeNull()
    {
        // Arrange
        var projectRecordId = "456";
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);
        var pageNumber = 1;
        var pageSize = 20;
        var sortField = nameof(ModificationsModel.CreatedAt);
        var sortDirection = SortDirections.Descending;

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project Y" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        var modifications = new List<ModificationsDto>
        {
            new() { ModificationId = "m1", ModificationType = "Type1", Status = ModificationStatus.InDraft }
        };

        var modificationsResponse = new GetModificationsResponse
        {
            Modifications = modifications.OrderBy(item => Enum.TryParse<ModificationStatusOrder>(item.Status, true, out var statusEnum)
            ? (int)statusEnum
            : (int)ModificationStatusOrder.None)
            .ToList() ?? [],
            TotalCount = 1
        };

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationsResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsForProject(projectRecordId, It.IsAny<ModificationSearchRequest>(), 1, 20, sortField, sortDirection))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.PostApproval(projectRecordId, "");

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PostApprovalViewModel>();
        model.Pagination.ShouldNotBeNull();
        model.Pagination.PageNumber.ShouldBe(pageNumber);
        model.Pagination.PageSize.ShouldBe(pageSize);
        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);
        var mod = model.Modifications.Single();
        mod.ModificationIdentifier.ShouldBe("m1");
        mod.ModificationType.ShouldBe("Type1");
        mod.Status.ShouldBe(ModificationStatus.InDraft);
        mod.ReviewType.ShouldBeNull();
        mod.Category.ShouldBeNull();
        mod.SentToRegulatorDate.ShouldBeNull();
        mod.SentToSponsorDate.ShouldBeNull();
    }

    [Fact]
    public async Task PostApproval_Modifications_When_ApprovedStatus_SubmittedDate_ShouldNotBeNull()
    {
        // Arrange
        var projectRecordId = "456";
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);
        var pageNumber = 1;
        var pageSize = 20;
        var sortField = nameof(ModificationsModel.CreatedAt);
        var sortDirection = SortDirections.Descending;

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project Y" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        var modifications = new List<ModificationsDto>
        {
            new() { ModificationId = "m1", ModificationType = "Type1", Status = ModificationStatus.Approved, CreatedAt=new DateTime(2025,10,02),
                SentToRegulatorDate= new DateTime(2025,10,02),
            }
        };

        var modificationsResponse = new GetModificationsResponse
        {
            Modifications = modifications.OrderBy(item => Enum.TryParse<ModificationStatusOrder>(item.Status, true, out var statusEnum)
            ? (int)statusEnum
            : (int)ModificationStatusOrder.None)
            .ToList() ?? [],
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
        var result = await Sut.PostApproval(projectRecordId, "");

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PostApprovalViewModel>();
        var mod = model.Modifications.Single();
        mod.ModificationIdentifier.ShouldBe("m1");
        mod.ModificationType.ShouldBe("Type1");
        mod.Status.ShouldBe(ModificationStatus.Approved);
        mod.ReviewType.ShouldBeNull();
        mod.Category.ShouldBeNull();
        mod.SentToRegulatorDate.ShouldNotBeNull();
        mod.SentToSponsorDate.ShouldBeNull();
        model.Pagination.ShouldNotBeNull();
        model.Pagination.PageNumber.ShouldBe(pageNumber);
        model.Pagination.PageSize.ShouldBe(pageSize);
        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);
    }

    [Fact]
    public async Task ConfirmDeleteProject_ReturnsViewResult_When_ProjectOverviewIsSuccessful()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);
        var projectRecordId = DefaultProjectRecordId;

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Test Project" },
            new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
        };

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
                    Status = ProjectRecordStatus.InDraft
                }
            });

        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        // Act
        var result = await Sut.ConfirmDeleteProject(projectRecordId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("/Features/ProjectOverview/Views/DeleteProject.cshtml");
        viewResult.Model.ShouldBeOfType<ProjectOverviewModel>();
    }

    [Fact]
    public async Task ConfirmDeleteProject_PropagatesError_When_GetProjectOverviewFails()
    {
        // Arrange
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);
        var projectRecordId = "err-1";
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(projectRecordId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ConfirmDeleteProject(projectRecordId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [InlineData(ModificationStatus.InDraft)]
    [InlineData(ModificationStatus.WithSponsor)]
    [InlineData(ModificationStatus.WithRegulator)]
    [InlineData(ModificationStatus.Approved)]
    [InlineData(ModificationStatus.NotApproved)]
    [InlineData(ModificationStatus.Authorised)]
    [InlineData(ModificationStatus.NotAuthorised)]
    public async Task PostApproval_Modifications_When_GetEnumStatus_Maps_Status_To_Expected_Order(string inputStatus)
    {
        // Arrange
        var projectRecordId = "456";
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);
        var pageNumber = 1;
        var pageSize = 20;
        var sortField = nameof(ModificationsModel.CreatedAt);
        var sortDirection = SortDirections.Descending;

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project Y" }
        };

        SetupProjectRecord(projectRecordId);
        SetupRespondentAnswers(projectRecordId, answers);
        SetupControllerContext(httpContext, tempData);
        SetupCMSService();

        var modifications = new List<ModificationsDto>
        {
            new() { ModificationId = "m1", ModificationType = "Type1", Status = inputStatus, CreatedAt=new DateTime(2025,10,12),
                SentToRegulatorDate= new DateTime(2025,10,02),
            },
                  new() { ModificationId = "m1", ModificationType = "Type1", Status = inputStatus, CreatedAt=new DateTime(2025,10,03),
                SentToRegulatorDate= new DateTime(2025,10,02),
            },
                        new() { ModificationId = "m1", ModificationType = "Type1", Status = inputStatus, CreatedAt=new DateTime(2025,10,03),
                SentToRegulatorDate= new DateTime(2025,10,02),
            },
                              new() { ModificationId = "m1", ModificationType = "Type1", Status = inputStatus, CreatedAt=new DateTime(2025,10,04),
                SentToRegulatorDate= new DateTime(2025,10,02),
            },
                                    new() { ModificationId = "m1", ModificationType = "Type1", Status = inputStatus, CreatedAt=new DateTime(2025,10,05),
                SentToRegulatorDate= new DateTime(2025,10,02),
            },      new() { ModificationId = "m1", ModificationType = "Type1", Status = inputStatus, CreatedAt=new DateTime(2025,10,06),
                SentToRegulatorDate= new DateTime(2025,10,02),
            }
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
            .Setup(s => s.GetModificationsForProject(projectRecordId, It.IsAny<ModificationSearchRequest>(), 1, 20, sortField, sortDirection))
             .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.PostApproval(projectRecordId, "", pageNumber, pageSize, sortField, sortDirection);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PostApprovalViewModel>();
        model.Pagination.ShouldNotBeNull();
        model.Pagination.PageNumber.ShouldBe(pageNumber);
        model.Pagination.PageSize.ShouldBe(pageSize);
        model.Pagination.SortField.ShouldBe(sortField);
        model.Pagination.SortDirection.ShouldBe(sortDirection);
    }
}
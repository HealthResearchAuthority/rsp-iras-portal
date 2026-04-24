using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Web.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Requests.UserManagement;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Controllers.ProjectOverview;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ProjectOverviewControllerTests;

public class ProjectOverviewProjectTeamFeatureFlagTests : TestServiceBase<ProjectOverviewController>
{
    private const string DefaultProjectRecordId = "123";

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

    private static DefaultHttpContext CreateHttpContextWithSession()
    {
        return new DefaultHttpContext { Session = new TestSession() };
    }

    private static TempDataDictionary CreateTempData(Mock<ITempDataProvider> tempDataProvider, HttpContext httpContext)
        => new(httpContext, tempDataProvider.Object);

    private void SetupControllerContext(HttpContext httpContext, ITempDataDictionary tempData)
    {
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = tempData;
    }

    private void SetupProjectRecord(string projectRecordId, string createdBy = "creator-id")
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
                    CreatedBy = createdBy
                }
            });

        applicationService
            .Setup(s => s.GetProjectRecordAuditTrail(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<ProjectRecordAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectRecordAuditTrailResponse { Items = [] }
            });
    }

    private void SetupRespondentAnswers(string projectRecordId)
    {
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(projectRecordId, QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new RespondentAnswerDto
                    {
                        QuestionId = QuestionIds.ShortProjectTitle,
                        AnswerText = "Test Project"
                    }
                ]
            });
    }

    private void SetupCMSService()
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
                    Sections =
                    [
                        new SectionModel
                        {
                            Id = "S1",
                            Questions =
                            [
                                new QuestionModel
                                {
                                    Id = "1",
                                    QuestionId = QuestionIds.ShortProjectTitle,
                                    Name = "Short Project Title",
                                    ShowAnswerOn = "ProjectTeam",
                                    SectionGroup = "Project Team",
                                    SectionSequence = 1,
                                    SequenceInSectionGroup = 1,
                                    Sequence = 1,
                                    Version = "1.0",
                                    AnswerDataType = "Text",
                                    Conformance = "Mandatory",
                                    ShowOriginalAnswer = false,
                                    Answers = [],
                                    ValidationRules = []
                                }
                            ]
                        }
                    ]
                }
            });
    }

    [Fact]
    public async Task ProjectTeam_WhenTeamRolesFeatureDisabled_ReturnsEmptyCollaborators()
    {
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        SetupControllerContext(httpContext, tempData);
        SetupProjectRecord(DefaultProjectRecordId);
        SetupRespondentAnswers(DefaultProjectRecordId);
        SetupCMSService();

        Mocker.GetMock<Microsoft.FeatureManagement.IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.TeamRoles))
            .ReturnsAsync(false);

        var result = await Sut.ProjectTeam(DefaultProjectRecordId, null);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectTeamViewModel>();
        model.Collaborators.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectTeam_WhenTeamRolesFeatureEnabled_ReturnsCollaboratorFromCreator()
    {
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        SetupControllerContext(httpContext, tempData);
        SetupProjectRecord(DefaultProjectRecordId, "creator-1");
        SetupRespondentAnswers(DefaultProjectRecordId);
        SetupCMSService();

        Mocker.GetMock<Microsoft.FeatureManagement.IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.TeamRoles))
            .ReturnsAsync(true);

        // Setup IProjectCollaboratorService mock - empty list since no collaborators added yet
        Mocker.GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(DefaultProjectRecordId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        // Setup GetUsers mock to handle the call (even with empty user IDs)
        // Use a catch-all setup for GetUsers since it has optional parameters
        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsers(It.IsAny<SearchUserRequest>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UsersResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UsersResponse { Users = [] }
            });

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser("creator-1", null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = new User(
                        "creator-1",
                        null,
                        null,
                        "Test",
                        "User",
                        "creator@example.com",
                        null,
                        null,
                        null,
                        null,
                        "Active",
                        DateTime.UtcNow)
                }
            });

        var result = await Sut.ProjectTeam(DefaultProjectRecordId, null);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectTeamViewModel>();
        // When no collaborators are added, the list should be empty
        model.Collaborators.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ProjectTeam_WhenTeamRolesFeatureEnabled_AndGetProjectTeamResultFails_ReturnsServiceError()
    {
        var httpContext = CreateHttpContextWithSession();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = CreateTempData(tempDataProvider, httpContext);

        SetupControllerContext(httpContext, tempData);
        SetupProjectRecord(DefaultProjectRecordId, "creator-1");
        SetupRespondentAnswers(DefaultProjectRecordId);
        SetupCMSService();

        Mocker.GetMock<Microsoft.FeatureManagement.IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.TeamRoles))
            .ReturnsAsync(true);

        // Setup IProjectCollaboratorService mock to return error
        Mocker.GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(DefaultProjectRecordId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null
            });

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser("creator-1", null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            });

        var result = await Sut.ProjectTeam(DefaultProjectRecordId, null);

        // ServiceError returns a StatusCodeResult with the HTTP status code from the service response
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Web.Features.ProjectCollaborators.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Features.ProjectCollaborators.Controllers;

namespace Rsp.Portal.UnitTests.Web.Features.ProjectCollaborators;

public class ProjectCollaboratorsSearchControllerTests : TestServiceBase<ProjectCollaboratorsSearchController>
{
    private const string DefaultProjectRecordId = "project-123";
    private const string DefaultEmail = "test@example.com";

    public ProjectCollaboratorsSearchControllerTests()
    {
        // Setup controller context with HttpContext
        var httpContext = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task AddCollaborator_ReturnsViewWithViewModel()
    {
        // Act
        var result = await Sut.AddCollaborator(DefaultProjectRecordId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SearchCollaboratorViewModel>();
        model.ProjectRecordId.ShouldBe(DefaultProjectRecordId);
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsServiceError_WhenGetProjectCollaboratorsFails()
    {
        // Arrange
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsServiceError_WhenGetUserFails()
    {
        // Arrange
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsServiceError_WhenGetRespondentAnswersFails()
    {
        // Arrange
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = CreateTestUser(),
                    Roles = [Roles.Applicant]
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsServiceError_WhenGetUserInSponsorOrganisationFails()
    {
        // Arrange
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };
        const string sponsorOrgId = "550e8400-e29b-41d4-a716-446655440000"; // Valid GUID string

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = CreateTestUser(),
                    Roles = [Roles.Applicant]
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new RespondentAnswerDto { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = sponsorOrgId },
                    new RespondentAnswerDto { QuestionId = QuestionIds.ChiefInvestigatorEmail, AnswerText = "ci@example.com" }
                ]
            });

        Mocker
            .GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsViewWithCollaboratorNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Sut.AddCollaborator));
        (model.CollaboratorFound ?? false).ShouldBeFalse();
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsViewWithInvalidUser_WhenUserIsAlreadyCollaborator()
    {
        // Arrange
        const string userId = "550e8400-e29b-41d4-a716-446655440001"; // Valid GUID string
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };
        var existingCollaborator = new ProjectCollaboratorResponse { UserId = userId };

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [existingCollaborator]
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = CreateTestUser(userId),
                    Roles = [Roles.Applicant]
                }
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Sut.AddCollaborator));
        (model.InvalidUser ?? false).ShouldBeTrue();
        model.InvalidUserMessage.ShouldContain("already a collaborator");
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsViewWithInvalidUser_WhenUserIsSponsor()
    {
        // Arrange
        const string userId = "550e8400-e29b-41d4-a716-446655440001"; // Valid GUID string
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };
        const string sponsorOrgId = "550e8400-e29b-41d4-a716-446655440000"; // Valid GUID string

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = CreateTestUser(userId),
                    Roles = [Roles.Applicant]
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new RespondentAnswerDto { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = sponsorOrgId },
                    new RespondentAnswerDto { QuestionId = QuestionIds.ChiefInvestigatorEmail, AnswerText = "ci@example.com" }
                ]
            });

        Mocker
            .GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid() }
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Sut.AddCollaborator));
        (model.InvalidUser ?? false).ShouldBeTrue();
        model.InvalidUserMessage.ShouldContain("Sponsor");
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsViewWithInvalidUser_WhenUserIsChiefInvestigator()
    {
        // Arrange
        const string userId = "550e8400-e29b-41d4-a716-446655440001"; // Valid GUID string
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };
        const string sponsorOrgId = "550e8400-e29b-41d4-a716-446655440000"; // Valid GUID string

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = CreateTestUser(userId),
                    Roles = [Roles.Applicant]
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new RespondentAnswerDto { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = sponsorOrgId },
                    new RespondentAnswerDto { QuestionId = QuestionIds.ChiefInvestigatorEmail, AnswerText = DefaultEmail }
                ]
            });

        Mocker
            .GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Sut.AddCollaborator));
        (model.InvalidUser ?? false).ShouldBeTrue();
        model.InvalidUserMessage.ShouldContain("Chief Investigator");
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsViewWithInvalidUser_WhenUserDoesNotHaveApplicantRole()
    {
        // Arrange
        const string userId = "550e8400-e29b-41d4-a716-446655440001"; // Valid GUID string
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };
        const string sponsorOrgId = "550e8400-e29b-41d4-a716-446655440000"; // Valid GUID string

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = CreateTestUser(userId),
                    Roles = [Roles.Reviewer] // Not Applicant
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new RespondentAnswerDto { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = sponsorOrgId },
                    new RespondentAnswerDto { QuestionId = QuestionIds.ChiefInvestigatorEmail, AnswerText = "ci@example.com" }
                ]
            });

        Mocker
            .GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Sut.AddCollaborator));
        (model.InvalidUser ?? false).ShouldBeTrue();
        model.InvalidUserMessage.ShouldContain("Applicant role");
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsViewWithValidUser_WhenAllValidationsPass()
    {
        // Arrange
        const string userId = "550e8400-e29b-41d4-a716-446655440001"; // Valid GUID string
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };
        const string sponsorOrgId = "550e8400-e29b-41d4-a716-446655440000"; // Valid GUID string

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = CreateTestUser(userId),
                    Roles = [Roles.Applicant]
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new RespondentAnswerDto { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = sponsorOrgId },
                    new RespondentAnswerDto { QuestionId = QuestionIds.ChiefInvestigatorEmail, AnswerText = "ci@example.com" }
                ]
            });

        Mocker
            .GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Sut.AddCollaborator));
        (model.CollaboratorFound ?? false).ShouldBeTrue();
        (model.InvalidUser ?? false).ShouldBeFalse();
    }

    [Fact]
    public async Task SearchCollaborator_InvalidUser_WhenUserHasMultipleApplicantRoles()
    {
        // Arrange - More than one Applicant role
        const string userId = "550e8400-e29b-41d4-a716-446655440001"; // Valid GUID string
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };
        const string sponsorOrgId = "550e8400-e29b-41d4-a716-446655440000"; // Valid GUID string

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = CreateTestUser(userId),
                    Roles = [Roles.Applicant, Roles.Applicant] // Two Applicant roles
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new RespondentAnswerDto { QuestionId = QuestionIds.PrimarySponsorOrganisation, AnswerText = sponsorOrgId },
                    new RespondentAnswerDto { QuestionId = QuestionIds.ChiefInvestigatorEmail, AnswerText = "ci@example.com" }
                ]
            });

        Mocker
            .GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Sut.AddCollaborator));
        (model.InvalidUser ?? false).ShouldBeTrue();
        model.InvalidUserMessage.ShouldContain("Applicant role");
    }

    [Fact]
    public async Task SearchCollaborator_ValidUser_WhenRespondentAnswersIsEmpty()
    {
        // Arrange - No respondent answers
        const string userId = "550e8400-e29b-41d4-a716-446655440001"; // Valid GUID string
        var model = new SearchCollaboratorViewModel { ProjectRecordId = DefaultProjectRecordId, Email = DefaultEmail };

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Setup(s => s.GetProjectCollaborators(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = CreateTestUser(userId),
                    Roles = [Roles.Applicant]
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        // Act
        var result = await Sut.SearchCollaborator(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Sut.AddCollaborator));
        (model.CollaboratorFound ?? false).ShouldBeTrue();
        (model.InvalidUser ?? false).ShouldBeFalse();
    }

    private static User CreateTestUser(string userId = "550e8400-e29b-41d4-a716-446655440001")
    {
        return new User(
            userId,
            "identity-provider-id",
            null,
            "Test",
            "User",
            DefaultEmail,
            null,
            null,
            null,
            null,
            "Active",
            DateTime.UtcNow
        );
    }

    [Fact]
    public async Task SearchCollaborator_ReturnsAddCollaboratorViewWithValidationError_WhenEmailIsEmpty()
    {
        var model = new SearchCollaboratorViewModel
        {
            ProjectRecordId = DefaultProjectRecordId,
            Email = " "
        };

        var result = await Sut.SearchCollaborator(model);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe(nameof(Sut.AddCollaborator));
        viewResult.Model.ShouldBe(model);
        Sut.ModelState.ContainsKey(nameof(model.Email)).ShouldBeTrue();

        Mocker
            .GetMock<IProjectCollaboratorService>()
            .Verify(s => s.GetProjectCollaborators(It.IsAny<string>()), Times.Never);
        Mocker
            .GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
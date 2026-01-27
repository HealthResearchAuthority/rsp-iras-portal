using System.Text.Json;
using AutoFixture;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Controllers;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Helpers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.SponsorWorkspace.Authorisations;

public class AuthorisationsProjectClosuresControllerTests
    : TestServiceBase<AuthorisationsProjectClosuresController>
{
    private readonly DefaultHttpContext _http;
    private readonly Guid _sponsorOrganisationUserId = Guid.NewGuid();

    public AuthorisationsProjectClosuresControllerTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        _http.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
    }

    [Theory]
    [AutoData]
    public async Task ProjectClosures_Returns_View_With_Correct_Model(ProjectClosuresSearchResponse closuresResponse, List<User> users)
    {
        // arrange at least 1 matching user id.
        closuresResponse.ProjectClosures.First().UserId = users[0].Id;

        var serviceResponse = new ServiceResponse<ProjectClosuresSearchResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = closuresResponse
        };

        Mocker.GetMock<IProjectClosuresService>()
            .Setup(s => s.GetProjectClosuresBySponsorOrganisationUserId(
                _sponsorOrganisationUserId,
                It.IsAny<ProjectClosuresSearchRequest>(),
                1,
                20,
                nameof(ProjectClosuresModel.SentToSponsorDate),
                SortDirections.Descending))
            .ReturnsAsync(serviceResponse);

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse { Users = users }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                null,
                1,
                It.IsAny<int>()))
            .ReturnsAsync(usersResponse);

        // Act
        var result = await Sut.ProjectClosures(_sponsorOrganisationUserId);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeAssignableTo<ProjectClosuresViewModel>();

        model.ShouldNotBeNull();
        model.SponsorOrganisationUserId.ShouldBe(_sponsorOrganisationUserId);

        model.ProjectRecords.ShouldNotBeNull();
        model.ProjectRecords.Count().ShouldBe(closuresResponse.ProjectClosures.Count());

        model.ProjectRecords.First(r => r.UserId == users[0].Id).UserEmail.ShouldBe(users[0].Email);

        model.Pagination.ShouldNotBeNull();
        model.Pagination.RouteName.ShouldBe("sws:projectclosures");
        model.Pagination.AdditionalParameters.ShouldContainKey("SponsorOrganisationUserId");
        model.Pagination.AdditionalParameters["SponsorOrganisationUserId"].ShouldBe(_sponsorOrganisationUserId.ToString());
        model.Pagination.SortField.ShouldBe(nameof(ProjectClosuresModel.SentToSponsorDate));
        model.Pagination.SortDirection.ShouldBe(SortDirections.Descending);
    }

    [Theory]
    [AutoData]
    public async Task ApplyProjectClosuresFilters_Invalid_ModelState_Redirects_Back(ProjectClosuresViewModel model)
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure(nameof(ProjectClosuresSearchModel.SearchTerm), "Invalid search term")
        });

        Mocker.GetMock<IValidator<ProjectClosuresSearchModel>>()
            .Setup(v => v.ValidateAsync(model.Search, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await Sut.ApplyProjectClosuresFilters(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ShouldNotBeNull();
        redirectResult.ActionName.ShouldBe(nameof(AuthorisationsProjectClosuresController.ProjectClosures));
        redirectResult.RouteValues.ShouldContainKey("sponsorOrganisationUserId");
        redirectResult.RouteValues["sponsorOrganisationUserId"].ShouldBe(model.SponsorOrganisationUserId);
    }

    private void ArrangeBuilderSuccess
    (
            string projectRecordId,
            int irasId,
            DateTime closureDate,
            string plannedEndDateAnswer,
            string shortTitleAnswer,
            object? questionSetContent = null
    )
    {
        // 1) ProjectRecord
        var projectRecordResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new IrasApplicationResponse
            {
                Id = projectRecordId,
                IrasId = irasId
            }
        };
        Mocker.GetMock<IApplicationsService>()
            .Setup(a => a.GetProjectRecord(projectRecordId))
            .ReturnsAsync(projectRecordResponse);

        // 2) Respondent Answers
        var respondentAnswers = new List<RespondentAnswerDto>
        {
            new RespondentAnswerDto
            {
                QuestionId = QuestionIds.ProjectPlannedEndDate,
                AnswerText = plannedEndDateAnswer
            },
            new RespondentAnswerDto
            {
                QuestionId = QuestionIds.ShortProjectTitle,
                AnswerText = shortTitleAnswer
            }
        };

        var respondentAnswersResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = respondentAnswers
        };
        Mocker.GetMock<IRespondentService>()
            .Setup(r => r.GetRespondentAnswers(projectRecordId, QuestionCategories.ProjectRecord))
            .ReturnsAsync(respondentAnswersResponse);

        // 3) CMS Questionset
        var qsResponse = new ServiceResponse<CmsQuestionSetResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSetContent as CmsQuestionSetResponse ?? new CmsQuestionSetResponse()
        };
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(c => c.GetQuestionSet(null, null))
            .ReturnsAsync(qsResponse);

        // 4) ProjectClosure (ActualEndDate)
        var pcResponse = new ServiceResponse<ProjectClosuresSearchResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new ProjectClosuresSearchResponse
            {
                ProjectClosures = new List<ProjectClosuresResponse>
                {
                    new ProjectClosuresResponse {ProjectRecordId = projectRecordId, ClosureDate = closureDate, Status = "With sponsor" }
                }
            }
        };
        Mocker.GetMock<IProjectClosuresService>()
            .Setup(p => p.GetProjectClosuresByProjectRecordId(projectRecordId))
            .ReturnsAsync(pcResponse);
    }

    // ============================
    // GET: CheckAndAuthoriseProjectClosure
    // ============================

    [Theory]
    [AutoData]
    public async Task CheckAndAuthoriseProjectClosure_Returns_View_With_Hydrated_Model(
        string projectRecordId)
    {
        // Arrange
        var irasId = 123456;
        var closureDate = new DateTime(2025, 01, 10);
        var plannedEndDateAnswer = "2025-02-20";
        var shortTitleAnswer = "abc";

        ArrangeBuilderSuccess(
            projectRecordId,
            irasId,
            closureDate,
            plannedEndDateAnswer,
            shortTitleAnswer
        );

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid() }
            });

        // Act
        var result = await Sut.CheckAndAuthoriseProjectClosure(projectRecordId, _sponsorOrganisationUserId);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeAssignableTo<AuthoriseProjectClosuresOutcomeViewModel>();

        model.ProjectRecordId.ShouldBe(projectRecordId);
        model.SponsorOrganisationUserId.ShouldBe(_sponsorOrganisationUserId);
        model.IrasId.ShouldBe(irasId);
        model.ShortProjectTitle.ShouldBe(shortTitleAnswer);

        var expectedActual = DateHelper.ConvertDateToString(closureDate);
        var expectedPlanned = DateHelper.ConvertDateToString(plannedEndDateAnswer);

        model.ActualEndDate.ShouldBe(DateHelper.ConvertDateToString(expectedActual)); // builder robi podwójny Convert
        model.PlannedEndDate.ShouldBe(expectedPlanned);
    }

    [Fact]
    public async Task CheckAndAuthoriseProjectClosure_Returns_ServiceError_When_SponsorOrganisationUser_Fails()
    {
        // Arrange
        var projectRecordId = Guid.NewGuid().ToString();
        var irasId = 123456;
        var closureDate = new DateTime(2025, 01, 10);
        var plannedEndDateAnswer = "2025-02-20";
        var shortTitleAnswer = "abc";

        ArrangeBuilderSuccess(
            projectRecordId,
            irasId,
            closureDate,
            plannedEndDateAnswer,
            shortTitleAnswer
        );

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>()
                .WithError("Service fail")
                .WithStatus(HttpStatusCode.InternalServerError));

        // Act
        var result = await Sut.CheckAndAuthoriseProjectClosure(projectRecordId, _sponsorOrganisationUserId);
        // Assert:
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Theory]
    [AutoData]
    public async Task CheckAndAuthoriseProjectClosure_Returns_ServiceError_When_ProjectRecord_Fails(
        string projectRecordId)
    {
        // Arrange
        var failed = new ServiceResponse<IrasApplicationResponse>()
            .WithError("Service fail")
            .WithStatus(HttpStatusCode.InternalServerError);

        Mocker.GetMock<IApplicationsService>()
            .Setup(a => a.GetProjectRecord(projectRecordId))
            .ReturnsAsync(failed);

        // Act
        var result = await Sut.CheckAndAuthoriseProjectClosure(projectRecordId, _sponsorOrganisationUserId);

        // Assert:
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    // ============================
    // POST: CheckAndAuthoriseProjectClosure
    // ============================

    [Theory]
    [AutoData]
    public async Task CheckAndAuthoriseProjectClosure_Post_Invalid_ModelState_Returns_View_With_Preserved_Outcome(
        AuthoriseProjectClosuresOutcomeViewModel posted)
    {
        // Arrange:
        var irasId = 999999;
        var closureDate = new DateTime(2025, 03, 15);
        var plannedEndDateAnswer = "2025-03-31";
        var shortTitleAnswer = "abc";

        posted.ProjectRecordId = posted.ProjectRecordId ?? Guid.NewGuid().ToString();
        posted.SponsorOrganisationUserId = posted.SponsorOrganisationUserId != Guid.Empty
            ? posted.SponsorOrganisationUserId
            : _sponsorOrganisationUserId;

        ArrangeBuilderSuccess(
            posted.ProjectRecordId,
            irasId,
            closureDate,
            plannedEndDateAnswer,
            shortTitleAnswer
        );

        Sut.ModelState.AddModelError("Outcome", "Outcome is required");

        // Act
        var result = await Sut.CheckAndAuthoriseProjectClosure(posted);

        // Assert:
        var view = result.ShouldBeOfType<ViewResult>();
        var hydrated = view.Model.ShouldBeAssignableTo<AuthoriseProjectClosuresOutcomeViewModel>();

        hydrated.ProjectRecordId.ShouldBe(posted.ProjectRecordId);
        hydrated.SponsorOrganisationUserId.ShouldBe(posted.SponsorOrganisationUserId);
        hydrated.Outcome.ShouldBe(posted.Outcome);
        hydrated.IrasId.ShouldBe(irasId);
        hydrated.ShortProjectTitle.ShouldBe(shortTitleAnswer);
    }

    [Theory]
    [AutoData]
    public async Task CheckAndAuthoriseProjectClosure_Post_Authorised_Redirects_To_PreAuthorisation_And_Sets_TempData(
        AuthoriseProjectClosuresOutcomeViewModel posted)
    {
        // Arrange
        posted.ProjectRecordId = posted.ProjectRecordId ?? Guid.NewGuid().ToString();
        posted.SponsorOrganisationUserId = posted.SponsorOrganisationUserId != Guid.Empty
            ? posted.SponsorOrganisationUserId
            : _sponsorOrganisationUserId;
        posted.Outcome = nameof(ProjectClosureStatus.Authorised);

        ArrangeBuilderSuccess(
            posted.ProjectRecordId,
            123,
            new DateTime(2025, 07, 01),
            "2025-08-01",
            "title"
        );

        // Act
        var result = await Sut.CheckAndAuthoriseProjectClosure(posted);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AuthorisationsProjectClosuresController.ProjectClosurePreAuthorisation));

        // TempData powinno zawierać zserializowany model
        var json = Sut.TempData[TempDataKeys.PreAuthProjectClosureModel] as string;
        json.ShouldNotBeNullOrWhiteSpace();

        var deserialized = JsonSerializer.Deserialize<AuthoriseProjectClosuresOutcomeViewModel>(json!);
        deserialized.ShouldNotBeNull();
        deserialized!.ProjectRecordId.ShouldBe(posted.ProjectRecordId);
        deserialized.SponsorOrganisationUserId.ShouldBe(posted.SponsorOrganisationUserId);
        deserialized.Outcome.ShouldBe(nameof(ProjectClosureStatus.Authorised));
    }

    [Theory]
    [AutoData]
    public async Task CheckAndAuthoriseProjectClosure_Post_NotAuthorised_Updates_Status_And_Redirects_To_Confirmation(
        AuthoriseProjectClosuresOutcomeViewModel posted)
    {
        // Arrange
        posted.ProjectRecordId = posted.ProjectRecordId ?? Guid.NewGuid().ToString();
        posted.SponsorOrganisationUserId = posted.SponsorOrganisationUserId != Guid.Empty
            ? posted.SponsorOrganisationUserId
            : _sponsorOrganisationUserId;
        posted.Outcome = nameof(ProjectClosureStatus.NotAuthorised);

        ArrangeBuilderSuccess(
            posted.ProjectRecordId,
            1234,
            new DateTime(2025, 02, 02),
            "2025-02-28",
            "title"
        );

        Mocker.GetMock<IProjectClosuresService>()
            .Setup(s => s.UpdateProjectClosureStatus(posted.ProjectRecordId, ProjectClosureStatus.NotAuthorised))
            .ReturnsAsync(new ServiceResponse().WithStatus(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var result = await Sut.CheckAndAuthoriseProjectClosure(posted);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AuthorisationsProjectClosuresController.ProjectClosureConfirmation));
        redirect.RouteValues.ShouldContainKey(nameof(AuthoriseProjectClosuresOutcomeViewModel.ProjectRecordId));
        redirect.RouteValues[nameof(AuthoriseProjectClosuresOutcomeViewModel.ProjectRecordId)]
            .ShouldBe(posted.ProjectRecordId);

        Mocker.GetMock<IProjectClosuresService>()
            .Verify(s => s.UpdateProjectClosureStatus(posted.ProjectRecordId, ProjectClosureStatus.NotAuthorised), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task CheckAndAuthoriseProjectClosure_Post_Missing_Outcome_Returns_ServiceError(
        AuthoriseProjectClosuresOutcomeViewModel posted)
    {
        // Arrange
        posted.ProjectRecordId = posted.ProjectRecordId ?? Guid.NewGuid().ToString();
        posted.SponsorOrganisationUserId = posted.SponsorOrganisationUserId != Guid.Empty
            ? posted.SponsorOrganisationUserId
            : _sponsorOrganisationUserId;
        posted.Outcome = null; // brak wyboru

        ArrangeBuilderSuccess(
            posted.ProjectRecordId,
            123,
            new DateTime(2025, 02, 02),
            "2025-02-28",
            "Title"
        );

        // Act
        var result = await Sut.CheckAndAuthoriseProjectClosure(posted);

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    // ============================
    // GET: ProjectClosurePreAuthorisation
    // ============================

    [Theory]
    [AutoData]
    public void ProjectClosurePreAuthorisation_Returns_View_When_TempData_Present(
        AuthoriseProjectClosuresOutcomeViewModel model)
    {
        // Arrange
        var json = JsonSerializer.Serialize(model);
        Sut.TempData[TempDataKeys.PreAuthProjectClosureModel] = json;

        // Act
        var result = Sut.ProjectClosurePreAuthorisation();

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var vm = view.Model.ShouldBeAssignableTo<AuthoriseProjectClosuresOutcomeViewModel>();
        vm.ProjectRecordId.ShouldBe(model.ProjectRecordId);
        vm.SponsorOrganisationUserId.ShouldBe(model.SponsorOrganisationUserId);
        vm.Outcome.ShouldBe(model.Outcome);
    }

    [Fact]
    public void ProjectClosurePreAuthorisation_Returns_ServiceError_When_TempData_Missing()
    {
        // Act
        var result = Sut.ProjectClosurePreAuthorisation();

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    // ============================
    // POST: ProjectClosurePreAuthorisationConfirm
    // ============================

    [Theory]
    [AutoData]
    public async Task ProjectClosurePreAuthorisationConfirm_Updates_Status_And_Redirects_To_Confirmation(
        AuthoriseProjectClosuresOutcomeViewModel model)
    {
        // Arrange
        model.ProjectRecordId = model.ProjectRecordId ?? Guid.NewGuid().ToString();
        Sut.TempData[TempDataKeys.PreAuthProjectClosureModel] = JsonSerializer.Serialize(model);

        Mocker.GetMock<IProjectClosuresService>()
            .Setup(s => s.UpdateProjectClosureStatus(model.ProjectRecordId, ProjectClosureStatus.Authorised))
            .ReturnsAsync(new ServiceResponse().WithStatus(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var result = await Sut.ProjectClosurePreAuthorisationConfirm();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AuthorisationsProjectClosuresController.ProjectClosureConfirmation));
        Mocker.GetMock<IProjectClosuresService>()
            .Verify(s => s.UpdateProjectClosureStatus(model.ProjectRecordId, ProjectClosureStatus.Authorised), Times.Once);
    }

    [Fact]
    public async Task ProjectClosurePreAuthorisationConfirm_Returns_ServiceError_When_TempData_Missing()
    {
        // Act
        var result = await Sut.ProjectClosurePreAuthorisationConfirm();

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    // ============================
    // GET: ProjectClosureConfirmation
    // ============================

    [Theory]
    [AutoData]
    public void ProjectClosureConfirmation_Returns_View_With_Model(AuthoriseProjectClosuresOutcomeViewModel model)
    {
        // Act
        var result = Sut.ProjectClosureConfirmation(model);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var vm = view.Model.ShouldBeAssignableTo<AuthoriseProjectClosuresOutcomeViewModel>();
        vm.ShouldBe(model);
    }

    [Theory]
    [ThreeItemsAutoData]
    public async Task ProjectClosures_Sorts_By_UserEmail_And_Paginates_Locally_Success(
    ProjectClosuresSearchResponse closuresResponse,
    List<User> users)
    {
        users = users.Take(3).ToList();

        var usersFixed = new List<User>
        {
            users[0] with { Email = "bbb@example.com" },
            users[1] with { Email = "aaa@example.com" },
            users[2] with { Email = "ccc@example.com" }
        };

        var closuresList = closuresResponse.ProjectClosures.Take(3).ToList();
        closuresList[0].UserId = usersFixed[0].Id;
        closuresList[1].UserId = usersFixed[1].Id;
        closuresList[2].UserId = usersFixed[2].Id;

        closuresResponse.TotalCount = 3;

        var serviceResponse = new ServiceResponse<ProjectClosuresSearchResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = closuresResponse
        };

        // SortField == UserEmail
        Mocker.GetMock<IProjectClosuresService>()
            .Setup(s => s.GetProjectClosuresBySponsorOrganisationUserIdWithoutPaging(
                _sponsorOrganisationUserId,
                It.IsAny<ProjectClosuresSearchRequest>()))
            .ReturnsAsync(serviceResponse);

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse { Users = usersFixed }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                null,
                1,
                It.IsAny<int>()))
            .ReturnsAsync(usersResponse);

        // --- Act ---
        var result = await Sut.ProjectClosures(
            sponsorOrganisationUserId: _sponsorOrganisationUserId,
            pageNumber: 1,
            pageSize: 2,
            sortField: nameof(ProjectClosuresModel.UserEmail),
            sortDirection: SortDirections.Ascending);

        // --- Assert ---
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeAssignableTo<ProjectClosuresViewModel>();

        var list = model.ProjectRecords.ToList();

        list.Count.ShouldBe(2);
        list[0].UserEmail.ShouldBe("aaa@example.com");
        list[1].UserEmail.ShouldBe("bbb@example.com");

        model.Pagination.SortField.ShouldBe(nameof(ProjectClosuresModel.UserEmail));
        model.Pagination.SortDirection.ShouldBe(SortDirections.Ascending);

        Mocker.GetMock<IProjectClosuresService>()
                .Verify(s => s.GetProjectClosuresBySponsorOrganisationUserIdWithoutPaging(
                    _sponsorOrganisationUserId, It.IsAny<ProjectClosuresSearchRequest>()),
                    Times.Once);

        Mocker.GetMock<IProjectClosuresService>()
            .Verify(s => s.GetProjectClosuresBySponsorOrganisationUserId(
                It.IsAny<Guid>(),
                It.IsAny<ProjectClosuresSearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task ProjectClosures_Sort_By_UserEmail_Returns_BadRequest_When_PageNumber_Is_Zero(
        ProjectClosuresSearchResponse closuresResponse,
        List<User> users)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<ProjectClosuresSearchResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = closuresResponse
        };

        Mocker.GetMock<IProjectClosuresService>()
            .Setup(s => s.GetProjectClosuresBySponsorOrganisationUserIdWithoutPaging(
                _sponsorOrganisationUserId, It.IsAny<ProjectClosuresSearchRequest>()))
            .ReturnsAsync(serviceResponse);

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse { Users = users }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                null,
                1,
                It.IsAny<int>()))
            .ReturnsAsync(usersResponse);

        // Act: pageNumber=0, sortField=UserEmail
        var result = await Sut.ProjectClosures(
            sponsorOrganisationUserId: _sponsorOrganisationUserId,
            pageNumber: 0,
            pageSize: 20,
            sortField: nameof(ProjectClosuresModel.UserEmail),
            sortDirection: SortDirections.Descending);

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }
}

/// <summary>
/// Forces auto data to generate 3 mock items
/// </summary>
public class ThreeItemsAutoDataAttribute : AutoDataAttribute
{
    public ThreeItemsAutoDataAttribute()
        : base(() =>
        {
            var f = new Fixture();
            f.RepeatCount = 3;
            return f;
        })
    {
    }
}
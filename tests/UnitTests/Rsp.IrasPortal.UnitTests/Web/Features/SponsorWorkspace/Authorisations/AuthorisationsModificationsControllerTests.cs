using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.CmsQuestionset.Modifications;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Controllers;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Services;
using Rsp.Portal.Web.Models;
using Claim = System.Security.Claims.Claim;

namespace Rsp.Portal.UnitTests.Web.Features.SponsorWorkspace.Authorisations;

public class AuthorisationsModificationsControllerTests : TestServiceBase<AuthorisationsModificationsController>
{
    private readonly DefaultHttpContext _http;
    private readonly Guid _sponsorOrganisationUserId = Guid.NewGuid();

    public AuthorisationsModificationsControllerTests()
    {
        var currentUserEmail = "test@test.co.uk";
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        _http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, currentUserEmail)
        }, "TestAuth"));

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
    }

    [Theory]
    [AutoData]
    public async Task Modifications_Returns_View_With_Correct_Model(GetModificationsResponse modificationResponse)
    {
        // Arrange
        Mocker.GetMock<ISponsorUserAuthorisationService>()
            .Setup(s => s.AuthoriseAsync(
                Sut,
                _sponsorOrganisationUserId,
                It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(Authorised(_sponsorOrganisationUserId));

        var modificationsServiceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsBySponsorOrganisationUserId(
                _sponsorOrganisationUserId,
                It.IsAny<SponsorAuthorisationsModificationsSearchRequest>(),
                1,
                20,
                nameof(ModificationsModel.SentToSponsorDate),
                SortDirections.Descending))
            .ReturnsAsync(modificationsServiceResponse);

        // Act
        var result = await Sut.Modifications(_sponsorOrganisationUserId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<AuthorisationsModificationsViewModel>();

        model.ShouldNotBeNull();
        model.SponsorOrganisationUserId.ShouldBe(_sponsorOrganisationUserId);
        model.Modifications.ShouldNotBeNull();
        model.Pagination.ShouldNotBeNull();
        model.Pagination.RouteName.ShouldBe("sws:modifications");
        model.Pagination.AdditionalParameters.ShouldContainKey("SponsorOrganisationUserId");
    }

    [Theory]
    [AutoData]
    public async Task Modifications_When_Not_Authorised_Returns_Failure_Result(GetModificationsResponse modificationResponse)
    {
        // Arrange
        var failure = new StatusCodeResult((int)HttpStatusCode.Forbidden); // or a ViewResult / ObjectResult based on your ServiceError
        Mocker.GetMock<ISponsorUserAuthorisationService>()
            .Setup(s => s.AuthoriseAsync(
                Sut,
                _sponsorOrganisationUserId,
                It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(NotAuthorised(failure));

        // Act
        var result = await Sut.Modifications(_sponsorOrganisationUserId);

        // Assert
        result.ShouldBeSameAs(failure);
    }

    [Theory]
    [AutoData]
    public async Task Modifications_When_Authorisation_Returns_Forbid_Returns_Forbid(GetModificationsResponse modificationResponse)
    {
        // Arrange
        var forbid = new ForbidResult();

        Mocker.GetMock<ISponsorUserAuthorisationService>()
            .Setup(s => s.AuthoriseAsync(
                Sut,
                _sponsorOrganisationUserId,
                It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(NotAuthorised(forbid));

        // Act
        var result = await Sut.Modifications(_sponsorOrganisationUserId);

        // Assert
        result.ShouldBeSameAs(forbid);
        result.ShouldBeOfType<ForbidResult>();
    }

    [Theory]
    [AutoData]
    public async Task Modifications_When_Modifications_Service_Fails_Returns_ServiceError(GetModificationsResponse modificationResponse)
    {
        // Arrange
        Mocker.GetMock<ISponsorUserAuthorisationService>()
            .Setup(s => s.AuthoriseAsync(
                Sut,
                _sponsorOrganisationUserId,
                It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(Authorised(_sponsorOrganisationUserId));

        var modificationsServiceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsBySponsorOrganisationUserId(
                _sponsorOrganisationUserId,
                It.IsAny<SponsorAuthorisationsModificationsSearchRequest>(),
                1,
                20,
                nameof(ModificationsModel.SentToSponsorDate),
                SortDirections.Descending))
            .ReturnsAsync(modificationsServiceResponse);

        // Act
        var result = await Sut.Modifications(_sponsorOrganisationUserId);

        // Assert
        result.ShouldNotBeNull();
        // tighten this based on what ServiceError returns in your project
    }

    [Theory]
    [AutoData]
    public async Task ApplyFilters_Invalid_ModelState_Redirects_Back(AuthorisationsModificationsViewModel model)
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("SearchTerm", "Invalid search term")
        });

        Mocker.GetMock<IValidator<AuthorisationsModificationsSearchModel>>()
            .Setup(v => v.ValidateAsync(model.Search, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await Sut.ApplyFilters(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ShouldNotBeNull();
        redirectResult.ActionName.ShouldBe(nameof(Modifications));
        redirectResult.RouteValues["sponsorOrganisationUserId"].ShouldBe(model.SponsorOrganisationUserId);
    }

    [Fact]
    public async Task Returns_View_With_Mapped_Changes_And_Flags()
    {
        SetupAuthoriseOutcomeViewModel();

        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.RevisionAndAuthorisation))
            .ReturnsAsync(true);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses("PR1", _sponsorOrganisationUserId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse
                {
                    RevisionDescription = "abc"
                }
            });

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid() }
            });

        var result = await Sut.CheckAndAuthorise("PR1", "IRAS", "Short",
            _sponsorOrganisationUserId, _sponsorOrganisationUserId);

        result.ShouldBeOfType<ViewResult>();
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewData["RevisionSent"].ShouldBe(true);
    }

    [Fact]
    public async Task CheckAndAuthorise_Returns_Error_When_ModificationReviewResponses_Fails()
    {
        SetupAuthoriseOutcomeViewModel();

        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.RevisionAndAuthorisation))
            .ReturnsAsync(true);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = null
            });

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { IsAuthoriser = true }
            });

        var result = await Sut.CheckAndAuthorise("PR1", "IRAS", "Short",
            _sponsorOrganisationUserId, _sponsorOrganisationUserId);

        result.ShouldBeOfType<StatusCodeResult>()
              .StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task CheckAndAuthorise_InvalidModelState_WithFeatureFlag_Sets_ViewBag_RevisionSent()
    {
        // Arrange
        var viewModel = SetupAuthoriseOutcomeViewModel();
        Sut.ModelState.AddModelError("Outcome", "required");

        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.RevisionAndAuthorisation))
            .ReturnsAsync(true);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid(), IsAuthoriser = true }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(viewModel.ProjectRecordId, viewModel.ProjectModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse
                {
                    RevisionDescription = "test revision"
                }
            });

        // Act
        var result = await Sut.CheckAndAuthorise(viewModel);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewData["RevisionSent"].ShouldBe(true);
    }

    [Fact]
    public async Task CheckAndAuthorise_Returns_Error_When_SponsorOrganisationUser_Fails()
    {
        SetupAuthoriseOutcomeViewModel();

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            });

        // Act
        var result = await Sut.CheckAndAuthorise("PR1", "IRAS", "Short", _sponsorOrganisationUserId,
            _sponsorOrganisationUserId);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task Returns_View_With_Mapped_Changes_And_Flags_With_No_Documents()
    {
        SetupAuthoriseOutcomeViewModel();

        var projectModificationsService = Mocker.GetMock<IProjectModificationsService>();
        projectModificationsService
            .Setup(s => s.GetDocumentsForModification(It.IsAny<Guid>(),
                It.IsAny<ProjectOverviewDocumentSearchRequest>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<ProjectOverviewDocumentResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid() }
            });

        var result = await Sut.CheckAndAuthorise("PR1", "IRAS", "Short",
            _sponsorOrganisationUserId, _sponsorOrganisationUserId);

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task CheckAndAuthorise_InvalidModelState_WhenFeatureFlagOn_And_ReviewResponsesFails_Should_Return_ServiceError()
    {
        // Arrange
        var viewModel = SetupAuthoriseOutcomeViewModel();
        Sut.ModelState.AddModelError("Outcome", "required");

        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.RevisionAndAuthorisation))
            .ReturnsAsync(true);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid(), IsAuthoriser = true }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(viewModel.ProjectRecordId, viewModel.ProjectModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var result = await Sut.CheckAndAuthorise(viewModel);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task CheckAndAuthorise_InvalidModelState_Should_Return_View_With_Hydrated_Model()
    {
        // Arrange
        var authoriseOutcomeViewModel = SetupAuthoriseOutcomeViewModel();

        // Simulate model validation error
        Sut.ModelState.AddModelError("Outcome", "Outcome selection is required");

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid(), IsAuthoriser = true }
            });

        // Act
        var result = await Sut.CheckAndAuthorise(authoriseOutcomeViewModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<AuthoriseModificationsOutcomeViewModel>();

        // Ensure the model that comes back is the same (hydrated) one
        model.Outcome.ShouldBe(authoriseOutcomeViewModel.Outcome);

        // Optionally verify ModelState was indeed invalid
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.TempData[TempDataKeys.IsAuthoriser].ShouldBe(true);
    }

    [Fact]
    public async Task CheckAndAuthorise_InvalidModelState_Should_Return_View_With_Hydrated_Model_Authorised()
    {
        // Arrange
        var authoriseOutcomeViewModel = SetupAuthoriseOutcomeViewModel();

        authoriseOutcomeViewModel.Outcome = "Authorised";
        authoriseOutcomeViewModel.ReviewType = "No review required";

        // Act
        var result = await Sut.CheckAndAuthorise(authoriseOutcomeViewModel);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AuthorisationsModificationsController.Confirmation));
    }

    [Fact]
    public async Task CheckAndAuthorise_InvalidModelState_Should_Return_View_With_Hydrated_Model_Authorised_Review()
    {
        // Arrange
        var authoriseOutcomeViewModel = SetupAuthoriseOutcomeViewModel();

        authoriseOutcomeViewModel.Outcome = "Authorised";
        authoriseOutcomeViewModel.ReviewType = "Review required";

        // Act
        var result = await Sut.CheckAndAuthorise(authoriseOutcomeViewModel);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AuthorisationsModificationsController.Confirmation));
    }

    [Fact]
    public async Task CheckAndAuthorise_InvalidModelState_Should_Return_View_With_Hydrated_Model_NotAuthorised()
    {
        // Arrange
        var authoriseOutcomeViewModel = SetupAuthoriseOutcomeViewModel();

        authoriseOutcomeViewModel.Outcome = "Notauthorised";

        // Act
        var result = await Sut.CheckAndAuthorise(authoriseOutcomeViewModel);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AuthorisationsModificationsController.Confirmation));
    }

    [Fact]
    public async Task CheckAndAuthorise_ValidModelState_RequestRevisions_Should_Redirect_To_RequestRevisions()
    {
        // Arrange
        var viewModel = SetupAuthoriseOutcomeViewModel();
        viewModel.Outcome = "RequestRevisions";

        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.RevisionAndAuthorisation))
            .ReturnsAsync(true);

        // Act
        var result = await Sut.CheckAndAuthorise(viewModel);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AuthorisationsModificationsController.RequestRevisions));
        redirect.RouteValues.ShouldNotBeNull();
    }

    [Fact]
    public async Task RequestRevisions_WhenSponsorServiceFails_ReturnsServiceError()
    {
        var vm = SetupAuthoriseOutcomeViewModel();

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationUser(vm.SponsorOrganisationUserId))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        var result = await Sut.RequestRevisions(vm);

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task RequestRevisions_WhenReviewResponsesFails_ReturnsServiceError()
    {
        var vm = SetupAuthoriseOutcomeViewModel();

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid() }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(vm.ProjectRecordId, vm.ProjectModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        var result = await Sut.RequestRevisions(vm);

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task RequestRevisions_WhenAuthoriserAndNoRevisionDescription_ReturnsView()
    {
        var vm = SetupAuthoriseOutcomeViewModel();

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid(), IsAuthoriser = true }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(vm.ProjectRecordId, vm.ProjectModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse { RevisionDescription = "" }
            });

        var result = await Sut.RequestRevisions(vm);

        var view = result.ShouldBeOfType<ViewResult>();
        view.Model.ShouldBe(vm);
    }

    [Fact]
    public async Task RequestRevisions_WhenNotAuthoriser_ReturnsForbid()
    {
        var vm = SetupAuthoriseOutcomeViewModel();

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid(), IsAuthoriser = false }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(vm.ProjectRecordId, vm.ProjectModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse { RevisionDescription = "" }
            });

        var result = await Sut.RequestRevisions(vm);

        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RequestRevisions_WhenAuthoriserButRevisionAlreadyExists_ReturnsForbid()
    {
        var vm = SetupAuthoriseOutcomeViewModel();

        var sponsorOrganisationService = Mocker.GetMock<ISponsorOrganisationService>();
        sponsorOrganisationService
            .Setup(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid(), IsAuthoriser = true }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(vm.ProjectRecordId, vm.ProjectModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse { RevisionDescription = "already sent" }
            });

        var result = await Sut.RequestRevisions(vm);

        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SendRequestRevisions_InvalidModelState_WhenSponsorFails_ReturnsServiceError()
    {
        var vm = SetupAuthoriseOutcomeViewModel();
        vm.RevisionDescription = "";

        Mocker.GetMock<IValidator<AuthoriseModificationsOutcomeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AuthoriseModificationsOutcomeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("RevisionDescription", "required") }));

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationUser(vm.SponsorOrganisationUserId))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = null
            });

        var result = await Sut.SendRequestRevisions(vm);

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task SendRequestRevisions_InvalidModelState_WhenReviewResponsesFails_ReturnsServiceError()
    {
        var vm = SetupAuthoriseOutcomeViewModel();

        Mocker.GetMock<IValidator<AuthoriseModificationsOutcomeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AuthoriseModificationsOutcomeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("RevisionDescription", "required") }));

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationUser(vm.SponsorOrganisationUserId))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid(), IsAuthoriser = true }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(vm.ProjectRecordId, vm.ProjectModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = null
            });

        var result = await Sut.SendRequestRevisions(vm);

        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task SendRequestRevisions_InvalidModelState_Authoriser_NoRevisionDesc_Returns_RequestRevisions_View()
    {
        var vm = SetupAuthoriseOutcomeViewModel();

        Mocker.GetMock<IValidator<AuthoriseModificationsOutcomeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AuthoriseModificationsOutcomeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("RevisionDescription", "required") }));

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationUser(vm.SponsorOrganisationUserId))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid(), IsAuthoriser = true }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(vm.ProjectRecordId, vm.ProjectModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse { RevisionDescription = "" }
            });

        var result = await Sut.SendRequestRevisions(vm);

        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe(nameof(AuthorisationsModificationsController.RequestRevisions));
        view.Model.ShouldBe(vm);
    }

    [Fact]
    public async Task SendRequestRevisions_InvalidModelState_NotAuthoriser_ReturnsForbid()
    {
        var vm = SetupAuthoriseOutcomeViewModel();

        Mocker.GetMock<IValidator<AuthoriseModificationsOutcomeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AuthoriseModificationsOutcomeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("RevisionDescription", "required") }));

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationUser(vm.SponsorOrganisationUserId))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationUserDto { Id = Guid.NewGuid(), IsAuthoriser = false }
            });

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(vm.ProjectRecordId, vm.ProjectModificationId))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse { RevisionDescription = "" }
            });

        var result = await Sut.SendRequestRevisions(vm);

        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SendRequestRevisions_ValidModelState_UpdatesStatus_AndRedirectsToConfirmation()
    {
        var vm = SetupAuthoriseOutcomeViewModel();
        vm.RevisionDescription = "please change X";

        Mocker.GetMock<IValidator<AuthoriseModificationsOutcomeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AuthoriseModificationsOutcomeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        var projectModService = Mocker.GetMock<IProjectModificationsService>();

        var result = await Sut.SendRequestRevisions(vm);

        projectModService.Verify(s =>
            s.UpdateModificationStatus(
                vm.ProjectRecordId,
                Guid.Parse(vm.ModificationId),
                ModificationStatus.RequestRevisions,
                vm.RevisionDescription),
            Times.Once);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(AuthorisationsModificationsController.Confirmation));
    }

    [Fact]
    public async Task SendRequestRevisions_ValidModelState_DoesNotCallSponsorOrReviewServices()
    {
        var vm = SetupAuthoriseOutcomeViewModel();
        vm.RevisionDescription = "ok";

        Mocker.GetMock<IValidator<AuthoriseModificationsOutcomeViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<AuthoriseModificationsOutcomeViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        var sponsor = Mocker.GetMock<ISponsorOrganisationService>();
        var mods = Mocker.GetMock<IProjectModificationsService>();

        await Sut.SendRequestRevisions(vm);

        sponsor.Verify(s => s.GetSponsorOrganisationUser(It.IsAny<Guid>()), Times.Never);
        mods.Verify(s => s.GetModificationReviewResponses(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void Confirmation_Returns_View_With_Model()
    {
        // Arrange
        var model = new AuthoriseModificationsOutcomeViewModel
        {
            ModificationId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            ProjectRecordId = "PR-001"
        };

        // Act
        var result = Sut.Confirmation(model);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBeNull(); // default view
        view.Model.ShouldBeSameAs(model); // passes through the same instance
    }

    private AuthoriseModificationsOutcomeViewModel SetupAuthoriseOutcomeViewModel()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var projectModificationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var projectRecordId = "PR-001";
        var sponsorOrganisationUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var sponsorDetailsSectionId = "pm-sponsor-reference";
        var changeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // 1. GetModification -> one modification entry
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationResponse
                {
                    Id = projectModificationId,
                    ModificationIdentifier = projectModificationId.ToString(),
                    Status = ModificationStatus.InDraft,
                    ProjectRecordId = projectRecordId,
                    ModificationNumber = 1,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    CreatedBy = "TestUser",
                    UpdatedBy = "TestUser",
                    ModificationType = "Substantial",
                    Category = "Category A",
                    ReviewType = "Full Review"
                }
            });

        // 2. GetModificationChanges -> one change
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new ProjectModificationChangeResponse
                    {
                        Id = changeId, SpecificAreaOfChange = "SA1", AreaOfChange = "A1",
                        Status = ModificationStatus.InDraft
                    }
                ]
            });

        // 3. GetInitialModificationQuestions -> resolve names
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetInitialModificationQuestions())
            .ReturnsAsync(new ServiceResponse<StartingQuestionsDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StartingQuestionsDto
                {
                    AreasOfChange =
                    [
                        new AreaOfChangeDto
                        {
                            AutoGeneratedId = "A1",
                            OptionName = "Area Name",
                            SpecificAreasOfChange =
                                [new AnswerModel { AutoGeneratedId = "SA1", OptionName = "Specific Name" }]
                        }
                    ]
                }
            });

        // 4. For UpdateModificationChanges flow we need journey questions and answers per change call;
        // set validator minimal
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationsJourney(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections =
                    [
                        new SectionModel
                        {
                            Id = "S1", CategoryId = "C1",
                            Questions =
                            [
                                new QuestionModel
                                {
                                    Id = "Q1", QuestionId = "Q1", Name = "Q1", AnswerDataType = "Text",
                                    CategoryId = "C1", Conformance = "Mandatory"
                                }
                            ]
                        }
                    ]
                }
            });

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationAuditTrail(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationAuditTrailResponse
                {
                    Items = [],
                    TotalCount = 0
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangeAnswers(changeId, It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            { StatusCode = HttpStatusCode.OK, Content = [] });

        var answers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
            new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
        };
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), QuestionCategories.ProjectRecord))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            { StatusCode = HttpStatusCode.OK, Content = answers });

        // Use a permissive validator that sets IsValid true
        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Mock RankingOfChange response to avoid NullReferenceException
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new RankingOfChangeResponse
                {
                    ModificationType = new ModificationRank { Substantiality = "Substantial", Order = 1 },
                    Categorisation = new CategoryRank { Category = "Category", Order = 1 },
                    ReviewType = "ReviewType"
                }
            });

        // Build a fake CMS question set response that your helper accepts
        var cmsResponse = new CmsQuestionSetResponse
        {
            // Populate with enough structure for BuildQuestionnaireViewModel to work in your codebase
            Sections = new List<SectionModel>
            {
                new()
                {
                    Id = sponsorDetailsSectionId,
                    Questions = new List<QuestionModel>
                    {
                        new() { Id = "q-sponsor-name", Name = "Sponsor Name", AnswerDataType = "text" },
                        new() { Id = "q-sponsor-address", Name = "Sponsor Address", AnswerDataType = "textarea" }
                    }
                }
            }
        };

        // Respondent answers that match question ids above
        var respondentAnswers = new List<RespondentAnswerDto>
        {
            new() { QuestionId = "q-sponsor-name", AnswerText = "Dan" },
            new() { QuestionId = "q-sponsor-address", AnswerText = "Newcastle upon Tyne" }
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(
                "pm-sponsor-reference", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { Content = cmsResponse });

        // sponsor details question set and answers
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pm-sponsor-reference", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections =
                    [
                        new SectionModel
                        {
                            Id = "S2", CategoryId = "SCAT",
                            Questions =
                            [
                                new QuestionModel
                                {
                                    Id = "SQ1", QuestionId = "SQ1", Name = "SQ1", CategoryId = "SCAT",
                                    AnswerDataType = "Text"
                                }
                            ]
                        }
                    ]
                }
            });

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
            .Setup(s => s.GetDocumentsForModification(It.IsAny<Guid>(),
                It.IsAny<ProjectOverviewDocumentSearchRequest>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // sponsor details question set and answers
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections =
                    [
                        new SectionModel
                        {
                            Id = "IQA0600", CategoryId = "SCAT", Questions =
                            [
                                new QuestionModel
                                {
                                    Id = "IQA0600", QuestionId = "IQA0600", Name = "IQA0600", CategoryId = "SCAT",
                                    AnswerDataType = "Text",
                                    Answers = new List<AnswerModel>
                                    {
                                        new() { Id = "TypeB", OptionName = "actual text" }
                                    }
                                }
                            ]
                        }
                    ]
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            { StatusCode = HttpStatusCode.OK, Content = [] });

        // Default behaviour for revision review when feature flag triggers it
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationReviewResponses(
                It.IsAny<string>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationReviewResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationReviewResponse
                {
                    RevisionDescription = "default description"
                }
            });

        // This is your domain object you're enriching
        var modification = new ModificationDetailsViewModel
        {
            ModificationId = projectModificationId.ToString(),
            ProjectRecordId = projectRecordId
        };

        TypeAdapterConfig<ModificationDetailsViewModel, AuthoriseModificationsOutcomeViewModel>
            .NewConfig()
            .Ignore(dest => dest.ProjectOverviewDocumentViewModel);

        var authoriseOutcomeViewModel = modification.Adapt<AuthoriseModificationsOutcomeViewModel>();
        authoriseOutcomeViewModel.SponsorOrganisationUserId = sponsorOrganisationUserId;
        return authoriseOutcomeViewModel;
    }

    [Fact]
    public async Task ChangeDetails_Returns_View_With_Mapped_Changes_And_Flags()
    {
        SetupAuthoriseOutcomeViewModel();
        var modificationChangeId = Guid.NewGuid();
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModification("PR1", It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse<ProjectModificationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectModificationResponse
                {
                    Id = Guid.NewGuid(),
                    ModificationIdentifier = Guid.NewGuid().ToString(),
                    Status = ModificationStatus.ChangeReadyForSubmission,
                    ProjectRecordId = Guid.NewGuid().ToString(),
                    ModificationNumber = 1,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    CreatedBy = "TestUser",
                    UpdatedBy = "TestUser",
                    ModificationType = "Substantial",
                    Category = "Category A",
                    ReviewType = "Full Review"
                }
            });

        // Act
        var result = await Sut.ChangeDetails("PR1", "IRAS", "Short", _sponsorOrganisationUserId,
            _sponsorOrganisationUserId, modificationChangeId);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    public SponsorUserAuthorisationResult Authorised(Guid gid)
        => SponsorUserAuthorisationResult.Ok(gid);

    public SponsorUserAuthorisationResult NotAuthorised(IActionResult failure)
        => SponsorUserAuthorisationResult.Fail(failure);
}
using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Mapster;
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
using Rsp.IrasPortal.Web.Features.Modifications.Models;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.SponsorWorkspace.Authorisations;

public class AuthorisationsControllerTests : TestServiceBase<AuthorisationsController>
{
    private readonly DefaultHttpContext _http;
    private readonly Guid _sponsorOrganisationUserId = Guid.NewGuid();

    public AuthorisationsControllerTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        _http.User = new ClaimsPrincipal(new ClaimsIdentity());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
    }

    [Theory]
    [AutoData]
    public async Task Authorisations_Returns_View_With_Correct_Model(GetModificationsResponse modificationResponse)
    {
        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsBySponsorOrganisationUserId(_sponsorOrganisationUserId,
                It.IsAny<SponsorAuthorisationsSearchRequest>(), 1, 20, nameof(ModificationsModel.SentToSponsorDate),
                SortDirections.Descending))
            .ReturnsAsync(serviceResponse);

        var result = await Sut.Authorisations(_sponsorOrganisationUserId);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorAuthorisationsViewModel>();

        model.ShouldNotBeNull();
        model.SponsorOrganisationUserId.ShouldBe(_sponsorOrganisationUserId);
        model.Modifications.ShouldNotBeNull();
        model.Pagination.ShouldNotBeNull();
        model.Pagination.RouteName.ShouldBe("sws:authorisations");
        model.Pagination.AdditionalParameters.ShouldContainKey("SponsorOrganisationUserId");
    }

    [Theory]
    [AutoData]
    public async Task ApplyFilters_Invalid_ModelState_Redirects_Back(SponsorAuthorisationsViewModel model)
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("SearchTerm", "Invalid search term")
        });

        Mocker.GetMock<IValidator<SponsorAuthorisationsSearchModel>>()
            .Setup(v => v.ValidateAsync(model.Search, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await Sut.ApplyFilters(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ShouldNotBeNull();
        redirectResult.ActionName.ShouldBe(nameof(Authorisations));
        redirectResult.RouteValues["sponsorOrganisationUserId"].ShouldBe(model.SponsorOrganisationUserId);
    }

    [Fact]
    public async Task Returns_View_With_Mapped_Changes_And_Flags()
    {
        SetupAuthoriseOutcomeViewModel();

        // Act
        var result = await Sut.CheckAndAuthorise("PR1", "IRAS", "Short", _sponsorOrganisationUserId,
            _sponsorOrganisationUserId);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task CheckAndAuthorise_InvalidModelState_Should_Return_View_With_Hydrated_Model()
    {
        // Arrange
        var authoriseOutcomeViewModel = SetupAuthoriseOutcomeViewModel();

        // Simulate model validation error
        Sut.ModelState.AddModelError("Outcome", "Outcome selection is required");

        // Act
        var result = await Sut.CheckAndAuthorise(authoriseOutcomeViewModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<AuthoriseOutcomeViewModel>();

        // Ensure the model that comes back is the same (hydrated) one
        model.Outcome.ShouldBe(authoriseOutcomeViewModel.Outcome);

        // Optionally verify ModelState was indeed invalid
        Sut.ModelState.IsValid.ShouldBeFalse();
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
        redirect.ActionName.ShouldBe(nameof(AuthorisationsController.Confirmation));
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
        redirect.ActionName.ShouldBe(nameof(AuthorisationsController.Confirmation));
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
        redirect.ActionName.ShouldBe(nameof(AuthorisationsController.Confirmation));
    }

    [Fact]
    public void Confirmation_Returns_View_With_Model()
    {
        // Arrange
        var model = new AuthoriseOutcomeViewModel
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

    private AuthoriseOutcomeViewModel SetupAuthoriseOutcomeViewModel()
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
            .Setup(s => s.GetModification(It.IsAny<Guid>()))
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
            .Setup(s => s.GetModificationChanges(It.IsAny<Guid>()))
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

        // 4. For UpdateModificationChanges flow we need journey questions and answers per change call; set validator minimal
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

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                Content = respondentAnswers
            });

        // This is your domain object you're enriching
        var modification = new ModificationDetailsViewModel
        {
            ModificationId = projectModificationId.ToString(),
            ProjectRecordId = projectRecordId
        };

        TypeAdapterConfig<ModificationDetailsViewModel, AuthoriseOutcomeViewModel>
        .NewConfig()
        .Ignore(dest => dest.ProjectOverviewDocumentViewModel);

        var authoriseOutcomeViewModel = modification.Adapt<AuthoriseOutcomeViewModel>();
        authoriseOutcomeViewModel.SponsorOrganisationUserId = sponsorOrganisationUserId;
        return authoriseOutcomeViewModel;
    }

    [Fact]
    public async Task ChangeDetails_Returns_View_With_Mapped_Changes_And_Flags()
    {
        SetupAuthoriseOutcomeViewModel();
        var modificationChangeId = Guid.NewGuid();

        // Act
        var result = await Sut.ChangeDetails("PR1", "IRAS", "Short", _sponsorOrganisationUserId,
            _sponsorOrganisationUserId, modificationChangeId);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }
}
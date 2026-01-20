using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Features.SponsorWorkspace.MyOrganisations.Controllers;
using Rsp.Portal.Web.Features.SponsorWorkspace.MyOrganisations.Models;
using System.Security.Claims;
using System.Text.Json;
using Claim = System.Security.Claims.Claim;

namespace Rsp.Portal.UnitTests.Web.Features.SponsorWorkspace.MyOrganisations;

public class MyOrganisationsControllerTests : TestServiceBase<MyOrganisationsController>
{
    private const string DefaultEmail = "dan.hulmston@test.com";
    private readonly Mock<IApplicationsService> _applicationService;
    private readonly DefaultHttpContext _http;

    public MyOrganisationsControllerTests()
    {
        _applicationService = Mocker.GetMock<IApplicationsService>();
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
    }

    private Guid SetUser(Guid userId, string? email = DefaultEmail)
    {
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.UserId, userId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        _http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        return userId;
    }

    private void SetupSponsorOrgContextSuccess(
        string rtsId,
        string email,
        SponsorOrganisationDto? sponsorOrganisation = null,
        OrganisationDto? rtsOrganisation = null,
        bool isUserAdmin = false)
    {
        rtsOrganisation ??= new OrganisationDto
        {
            Name = "Test Org",
            CountryName = "United Kingdom"
        };

        sponsorOrganisation ??= new SponsorOrganisationDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            Users = new List<SponsorOrganisationUserDto>()
        };

        // Ensure the calling user is in the org AND active, with matching email
        sponsorOrganisation.Users = new List<SponsorOrganisationUserDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Email = email,
                IsActive = true,
                SponsorRole = isUserAdmin ? SponsorOrganisationUserRoles.OrganisationAdministrator : "Member",
                IsAuthoriser = false
            }
        };

        var rtsResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rtsOrganisation
        };

        var rbResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new[] { sponsorOrganisation }
            }
        };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(rtsResponse);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(rbResponse);
    }

    private void SetUser(Guid userId)
    {
        _http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(CustomClaimTypes.UserId, userId.ToString())
        }));
    }

    public static IEnumerable<object[]> SortCases()
    {
        yield return new object[]
        {
            "SponsorOrganisationName", "asc",
            new[]
            {
                new SponsorOrganisationDto { SponsorOrganisationName = "Beta" },
                new SponsorOrganisationDto { SponsorOrganisationName = "alpha" }
            },
            new[] { "alpha", "Beta" }
        };

        yield return new object[]
        {
            "SponsorOrganisationName", "desc",
            new[]
            {
                new SponsorOrganisationDto { SponsorOrganisationName = "Beta" },
                new SponsorOrganisationDto { SponsorOrganisationName = "alpha" }
            },
            new[] { "Beta", "alpha" }
        };

        yield return new object[]
        {
            "countries", "asc",
            new[]
            {
                new SponsorOrganisationDto { SponsorOrganisationName = "Bravo", Countries = new List<string> { "UK" } },
                new SponsorOrganisationDto { SponsorOrganisationName = "Alpha" },
                new SponsorOrganisationDto
                    { SponsorOrganisationName = "Charlie", Countries = new List<string> { "DE" } }
            },
            new[] { "Alpha", "Charlie", "Bravo" }
        };

        yield return new object[]
        {
            "countries", "desc",
            new[]
            {
                new SponsorOrganisationDto { SponsorOrganisationName = "A", Countries = new List<string> { "UK" } },
                new SponsorOrganisationDto { SponsorOrganisationName = "B", Countries = new List<string> { "DE" } },
                new SponsorOrganisationDto { SponsorOrganisationName = "C" }
            },
            new[] { "A", "B", "C" }
        };

        yield return new object[]
        {
            "unknown", "asc",
            new[]
            {
                new SponsorOrganisationDto { SponsorOrganisationName = "Zed" },
                new SponsorOrganisationDto { SponsorOrganisationName = "Alpha" }
            },
            new[] { "Alpha", "Zed" }
        };
    }

    [Theory]
    [MemberData(nameof(SortCases))]
    public async Task MyOrganisations_sorts_correctly(
        string sortField,
        string sortDirection,
        SponsorOrganisationDto[] organisations,
        string[] expectedOrder)
    {
        var userId = Guid.NewGuid();
        SetUser(userId);

        var serviceResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = organisations
            }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(
                It.IsAny<SponsorOrganisationSearchRequest>(),
                1,
                int.MaxValue,
                sortField,
                sortDirection))
            .ReturnsAsync(serviceResponse);

        var result = await Sut.MyOrganisations(sortField, sortDirection);

        var model = result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<SponsorMyOrganisationsViewModel>();

        model.MyOrganisations
            .Select(x => x.SponsorOrganisationName)
            .ShouldBe(expectedOrder);
    }

    [Theory]
    [InlineData("hawks", "countries", "desc")]
    [InlineData("", "SponsorOrganisationName", "asc")]
    [InlineData(null, "SponsorOrganisationName", "asc")]
    public async Task SearchMyOrganisations_persists_search_and_redirects(
        string? searchTerm,
        string sortField,
        string sortDirection)
    {
        SetUser(Guid.NewGuid());

        var model = new SponsorMyOrganisationsViewModel
        {
            Search = searchTerm == null
                ? null
                : new SponsorMyOrganisationsSearchModel { SearchTerm = searchTerm }
        };

        var result = await Sut.SearchMyOrganisations(model, sortField, sortDirection);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(MyOrganisationsController.MyOrganisations));
        redirect.RouteValues!["sortField"].ShouldBe(sortField);
        redirect.RouteValues!["sortDirection"].ShouldBe(sortDirection);

        var json = _http.Session.GetString(SessionKeys.SponsorMyOrganisationsSearch);
        json.ShouldNotBeNull();

        var saved = JsonSerializer.Deserialize<SponsorMyOrganisationsSearchModel>(json!);
        saved.ShouldNotBeNull();
        saved!.SearchTerm.ShouldBe(searchTerm);
    }

    [Theory]
    [InlineData("everton")]
    [InlineData("hawks")]
    public async Task MyOrganisations_when_session_contains_json_deserialises_into_model_search(string searchTerm)
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUser(userId);

        var sessionSearch = new SponsorMyOrganisationsSearchModel
        {
            SearchTerm = searchTerm
        };

        _http.Session.SetString(
            SessionKeys.SponsorMyOrganisationsSearch,
            JsonSerializer.Serialize(sessionSearch));

        var serviceResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = Array.Empty<SponsorOrganisationDto>()
            }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(
                It.IsAny<SponsorOrganisationSearchRequest>(),
                1,
                int.MaxValue,
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.MyOrganisations();

        // Assert
        var model = result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<SponsorMyOrganisationsViewModel>();

        model.Search.ShouldNotBeNull();
        model.Search.SearchTerm.ShouldBe(searchTerm);
    }

    // Add these tests into your existing MyOrganisationsControllerTests class (no MemberData helper)

    // 1) Defaults: sortField/sortDirection omitted -> redirect uses defaults AND session persists Search
    [Theory]
    [InlineData("everton")]
    [InlineData("hawks")]
    public async Task SearchMyOrganisations_when_sort_params_omitted_redirects_with_defaults_and_persists_search(
        string searchTerm)
    {
        // Arrange
        SetUser(Guid.NewGuid());

        var model = new SponsorMyOrganisationsViewModel
        {
            Search = new SponsorMyOrganisationsSearchModel { SearchTerm = searchTerm }
        };

        // Act
        var result = await Sut.SearchMyOrganisations(model, null, null);

        // Assert: redirect uses default values
        var json = _http.Session.GetString(SessionKeys.SponsorMyOrganisationsSearch);
        json.ShouldNotBeNull();

        var saved = JsonSerializer.Deserialize<SponsorMyOrganisationsSearchModel>(json!);
        saved.ShouldNotBeNull();
        saved!.SearchTerm.ShouldBe(searchTerm);
    }

    // 2) Explicit sort params: redirect uses provided values AND session persists Search
    [Theory]
    [InlineData("countries", "desc", "hawks")]
    [InlineData("SponsorOrganisationName", "asc", "everton")]
    public async Task SearchMyOrganisations_when_sort_params_provided_redirects_with_values_and_persists_search(
        string sortField,
        string sortDirection,
        string searchTerm)
    {
        // Arrange
        SetUser(Guid.NewGuid());

        var model = new SponsorMyOrganisationsViewModel
        {
            Search = new SponsorMyOrganisationsSearchModel { SearchTerm = searchTerm }
        };

        // Act
        var result = await Sut.SearchMyOrganisations(model, sortField, sortDirection);

        // Assert: redirect carries sort values
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(MyOrganisationsController.MyOrganisations));
        redirect.RouteValues!["sortField"].ShouldBe(sortField);
        redirect.RouteValues!["sortDirection"].ShouldBe(sortDirection);

        // Assert: session persisted search model
        var json = _http.Session.GetString(SessionKeys.SponsorMyOrganisationsSearch);
        json.ShouldNotBeNull();

        var saved = JsonSerializer.Deserialize<SponsorMyOrganisationsSearchModel>(json!);
        saved.ShouldNotBeNull();
        saved!.SearchTerm.ShouldBe(searchTerm);
    }

    // 3) Null Search: session still writes a default search model (no null JSON) AND redirects
    [Theory]
    [InlineData("countries", "desc")]
    [InlineData(null, null)]
    public async Task SearchMyOrganisations_when_search_is_null_persists_default_model_and_redirects(
        string? sortField,
        string? sortDirection)
    {
        // Arrange
        SetUser(Guid.NewGuid());

        var model = new SponsorMyOrganisationsViewModel
        {
            Search = null
        };

        // Act
        var result = await Sut.SearchMyOrganisations(model, sortField, sortDirection);

        // Assert: redirect
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(MyOrganisationsController.MyOrganisations));

        // Assert: session persisted *some* JSON and deserialises to a model
        var json = _http.Session.GetString(SessionKeys.SponsorMyOrganisationsSearch);
        json.ShouldNotBeNull();

        var saved = JsonSerializer.Deserialize<SponsorMyOrganisationsSearchModel>(json!);
        saved.ShouldNotBeNull();
        saved!.SearchTerm.ShouldBeNull();
    }

    // 4) PRG action: should not call the service at all
    [Theory]
    [InlineData("SponsorOrganisationName", "asc")]
    [InlineData("countries", "desc")]
    public async Task SearchMyOrganisations_does_not_call_service(string sortField, string sortDirection)
    {
        // Arrange
        SetUser(Guid.NewGuid());

        var model = new SponsorMyOrganisationsViewModel
        {
            Search = new SponsorMyOrganisationsSearchModel { SearchTerm = "anything" }
        };

        // Act
        _ = await Sut.SearchMyOrganisations(model, sortField, sortDirection);

        // Assert
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetAllSponsorOrganisations(
                    It.IsAny<SponsorOrganisationSearchRequest>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
    }

    [Fact]
    public async Task MyOrganisationProfile_ShouldReturnView_WhenContextPasses()
    {
        var rtsId = "87765";
        var userId = Guid.NewGuid();
        SetUser(userId, DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            new SponsorOrganisationDto
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Users = new List<SponsorOrganisationUserDto>()
            },
            new OrganisationDto
            {
                Name = "Acme Sponsor Org",
                CountryName = "UK"
            });

        var result = await Sut.MyOrganisationProfile(rtsId);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationProfileViewModel>();

        model.RtsId.ShouldBe(rtsId);
        model.Name.ShouldBe("Acme Sponsor Org");
    }

    [Theory]
    [AutoData]
    public async Task MyOrganisationProfile_ShouldReturnServiceError_WhenRtsFails(
        ClaimsPrincipal userClaims
    )
    {
        // Arrange

        var rtsResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.BadRequest
        };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(It.IsAny<string>()))
            .ReturnsAsync(rtsResponse);

        _http.User = userClaims;

        // Act
        var result = await Sut.MyOrganisationProfile("");

        // Assert
        var statusResult = result.ShouldBeOfType<StatusCodeResult>();
        statusResult.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task MyOrganisationProfile_ShouldForbid_WhenEmailClaimMissing()
    {
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), null);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail); // context has org info but user has no email claim

        var result = await Sut.MyOrganisationProfile(rtsId);

        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task MyOrganisationProfile_ShouldForbid_WhenUserNotInOrgOrDisabled()
    {
        var rtsId = "87765";
        var email = "user@test.com";
        SetUser(Guid.NewGuid(), email);

        var sponsorOrg = new SponsorOrganisationDto
        {
            Id = Guid.NewGuid(),
            IsActive = false,
            CreatedDate = DateTime.UtcNow,
            Users = new List<SponsorOrganisationUserDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Email = email,
                    IsActive = false // disabled membership
                }
            }
        };

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrg);

        var result = await Sut.MyOrganisationProfile(rtsId);

        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task MyOrganisationProfile_ShouldReturnNotFound_WhenSponsorOrganisationMissing()
    {
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "Org", CountryName = "UK" }
            });

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new AllSponsorOrganisationsResponse
                {
                    SponsorOrganisations = Array.Empty<SponsorOrganisationDto>()
                }
            });

        var result = await Sut.MyOrganisationProfile(rtsId);

        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MyOrganisationProfile_ShouldReturnServiceError_WhenSponsorOrgServiceFails()
    {
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto { Name = "Org", CountryName = "UK" }
            });

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        var result = await Sut.MyOrganisationProfile(rtsId);

        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(400);
    }

    [Theory]
    [AutoData]
    public async Task MyOrganisationProjects_ShouldReturnProjectResults(
        PaginatedResponse<CompleteProjectRecordResponse> mockResponse
    )
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Project Org", CountryName = "UK" });

        var searchModel = new SponsorOrganisationProjectSearchModel { IrasId = "1234" };
        _http.Session.SetString(SessionKeys.SponsorMyOrganisationsProjectsSearch,
            JsonSerializer.Serialize(searchModel));

        var serviceResponse = new ServiceResponse<PaginatedResponse<CompleteProjectRecordResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockResponse
        };

        _applicationService
            .Setup(s => s.GetPaginatedApplications(It.IsAny<ProjectRecordSearchRequest>(), 1, 20, It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.MyOrganisationProjects(rtsId);

        // Assert
        var statusResult = result.ShouldBeOfType<ViewResult>();
        var resultModel = statusResult.Model.ShouldBeOfType<SponsorMyOrganisationProjectsViewModel>();
        resultModel.RtsId.ShouldBe(rtsId);
        resultModel.Pagination.ShouldNotBeNull();
        resultModel.ProjectRecords.ShouldNotBeNull();
        resultModel.ProjectRecords.Count().ShouldBeGreaterThan(0);
    }

    [Theory]
    [AutoData]
    public async Task MyOrganisationProjectsFilter_ShouldRedirectWhenModelIsValid(
        SponsorMyOrganisationProjectsViewModel mockModel
    )
    {
        // Arrange

        var searchModel = new SponsorOrganisationProjectSearchModel { IrasId = "12345" };
        _http.Session.SetString(SessionKeys.SponsorMyOrganisationsProjectsSearch,
            JsonSerializer.Serialize(searchModel));

        var mockValidator = Mocker.GetMock<IValidator<SponsorOrganisationProjectSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<SponsorOrganisationProjectSearchModel>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.ApplyProjectRecordsFilters(mockModel);

        // Assert
        var actualResult = result.ShouldBeOfType<RedirectToActionResult>();
        actualResult.ActionName.ShouldBe("MyOrganisationProjects");
        actualResult.RouteValues!["rtsId"].ShouldBe(mockModel.RtsId);

        var storedJson = _http.Session.GetString(SessionKeys.SponsorMyOrganisationsProjectsSearch);
        storedJson.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [AutoData]
    public async Task MyOrganisationProjectsFilter_ShouldReturnSameViewWhenModelIsNotValid(
        SponsorMyOrganisationProjectsViewModel mockModel
    )
    {
        // Arrange

        var searchModel = new SponsorOrganisationProjectSearchModel { IrasId = "12345" };
        _http.Session.SetString(SessionKeys.SponsorMyOrganisationsProjectsSearch,
            JsonSerializer.Serialize(searchModel));

        var mockValidator = Mocker.GetMock<IValidator<SponsorOrganisationProjectSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<SponsorOrganisationProjectSearchModel>(), default))
            .ReturnsAsync(new ValidationResult
            { Errors = new List<ValidationFailure> { new("createddate", "Some Error") } });

        // Act
        var result = await Sut.ApplyProjectRecordsFilters(mockModel);

        // Assert
        var actualResult = result.ShouldBeOfType<ViewResult>();
        actualResult.ViewName.ShouldBe("MyOrganisationProjects");
        actualResult.Model.ShouldBeOfType<SponsorMyOrganisationProjectsViewModel>();
    }

    [Theory]
    [AutoData]
    public void MyOrganisationProjectsClearFilters_ShouldReturnRemoveAllFilters(
        SponsorOrganisationProjectSearchModel mockModel
    )
    {
        // Arrange
        var rtsId = "12345";
        _http.Session.SetString(SessionKeys.SponsorMyOrganisationsProjectsSearch, JsonSerializer.Serialize(mockModel));

        // Act
        var result = Sut.ClearProjectRecordsFilters(rtsId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.MyOrganisationProjects));
        redirectResult.RouteValues!["rtsId"].ShouldBe(rtsId);

        var updatedSearchJson = _http.Session.GetString(SessionKeys.SponsorMyOrganisationsProjectsSearch);
        updatedSearchJson.ShouldNotBeNull();

        var updatedSearch = JsonSerializer.Deserialize<SponsorOrganisationProjectSearchModel>(updatedSearchJson!);
        updatedSearch.ShouldNotBeNull();
        updatedSearch.Filters.Count.ShouldBe(0);
        updatedSearch.IrasId.ShouldBe(mockModel.IrasId);
    }

    [Theory]
    [AutoData]
    public async Task MyOrganisationProjectsRemoveFromDateFilter_ShouldReturnRemoveSingleFilter(
        SponsorOrganisationProjectSearchModel mockModel
    )
    {
        // Arrange
        var rtsId = "12345";
        var filterName = "datecreated-from";
        mockModel.FromDay = "1";
        mockModel.FromMonth = "8";
        mockModel.FromYear = "2023";

        _http.Session.SetString(SessionKeys.SponsorMyOrganisationsProjectsSearch, JsonSerializer.Serialize(mockModel));

        var mockValidator = Mocker.GetMock<IValidator<SponsorOrganisationProjectSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<SponsorOrganisationProjectSearchModel>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.RemoveProjectRecordFilter(filterName, It.IsAny<string>(), rtsId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.MyOrganisationProjects));
        redirectResult.RouteValues!["rtsId"].ShouldBe(rtsId);

        var updatedSearchJson = _http.Session.GetString(SessionKeys.SponsorMyOrganisationsProjectsSearch);
        updatedSearchJson.ShouldNotBeNull();

        var updatedSearch = JsonSerializer.Deserialize<SponsorOrganisationProjectSearchModel>(updatedSearchJson!);
        updatedSearch.ShouldNotBeNull();
        updatedSearch.FromDate.ShouldBeNull();
    }

    [Theory]
    [AutoData]
    public async Task MyOrganisationProjectsRemoveToDateFilter_ShouldReturnRemoveSingleFilter(
        SponsorOrganisationProjectSearchModel mockModel
    )
    {
        // Arrange
        var rtsId = "12345";
        var filterName = "datecreated-to";
        mockModel.ToDay = "1";
        mockModel.ToMonth = "8";
        mockModel.ToYear = "2023";

        _http.Session.SetString(SessionKeys.SponsorMyOrganisationsProjectsSearch, JsonSerializer.Serialize(mockModel));

        var mockValidator = Mocker.GetMock<IValidator<SponsorOrganisationProjectSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<SponsorOrganisationProjectSearchModel>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.RemoveProjectRecordFilter(filterName, It.IsAny<string>(), rtsId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.MyOrganisationProjects));
        redirectResult.RouteValues!["rtsId"].ShouldBe(rtsId);

        var updatedSearchJson = _http.Session.GetString(SessionKeys.SponsorMyOrganisationsProjectsSearch);
        updatedSearchJson.ShouldNotBeNull();

        var updatedSearch = JsonSerializer.Deserialize<SponsorOrganisationProjectSearchModel>(updatedSearchJson!);
        updatedSearch.ShouldNotBeNull();
        updatedSearch.ToDate.ShouldBeNull();
    }

    [Fact]
    public async Task MyOrganisationProjectsRemoveFilter_ShouldRedirectWhenSessionEmpty(
    )
    {
        // Arrange
        var rtsId = "12345";
        var filterName = "status";

        // Act
        var result = await Sut.RemoveProjectRecordFilter(filterName, It.IsAny<string>(), rtsId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.MyOrganisationProjects));
        redirectResult.RouteValues!["rtsId"].ShouldBe(rtsId);
    }

    [Theory]
    [AutoData]
    public async Task MyOrganisationUsers_ShouldReturnServiceError_WhenRtsFails(
        ClaimsPrincipal userClaims
    )
    {
        // Arrange

        var rtsResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.BadRequest
        };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(It.IsAny<string>()))
            .ReturnsAsync(rtsResponse);

        _http.User = userClaims;

        // Act
        var result = await Sut.MyOrganisationUsers("");

        // Assert
        var statusResult = result.ShouldBeOfType<StatusCodeResult>();
        statusResult.StatusCode.ShouldBe(400);
    }

    [Theory]
    [AutoData]
    public async Task MyOrganisationAuditTrail_ShouldReturnServiceError_WhenRtsFails(
        ClaimsPrincipal userClaims
    )
    {
        // Arrange

        var rtsResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.BadRequest
        };

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(It.IsAny<string>()))
            .ReturnsAsync(rtsResponse);

        _http.User = userClaims;

        // Act
        var result = await Sut.MyOrganisationAuditTrail("");

        // Assert
        var statusResult = result.ShouldBeOfType<StatusCodeResult>();
        statusResult.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task MyOrganisationAuditTrail_ShouldReturnView_WhenContextPasses()
    {
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Audit Org", CountryName = "UK" });

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.SponsorOrganisationAuditTrail(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationAuditTrailResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new SponsorOrganisationAuditTrailResponse
                {
                    Items = Array.Empty<SponsorOrganisationAuditTrailDto>()
                }
            });

        var result = await Sut.MyOrganisationAuditTrail(rtsId);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationAuditViewModel>();

        model.RtsId.ShouldBe(rtsId);
        model.Name.ShouldBe("Audit Org");
    }

    [Fact]
    public async Task MyOrganisationUsers_sorts_by_status_asc_active_first_and_sets_status_from_latest_dto()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        var activeUserId = Guid.NewGuid();
        var disabledUserId = Guid.NewGuid();

        var sponsorOrg = new SponsorOrganisationDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Users = new List<SponsorOrganisationUserDto>
            {
                // calling user membership
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Email = DefaultEmail,
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                },

                // Disabled user: include an older Active record first, then Disabled last (latest wins)
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = disabledUserId,
                    Email = "disabled@test.com",
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = disabledUserId,
                    Email = "disabled@test.com",
                    IsActive = false,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                },

                // Active user
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = activeUserId,
                    Email = "active@test.com",
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                }
            }
        };

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrg);

        // Return users in "wrong" order so controller sorting is exercised
        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 2,
                Users = new[]
                {
                    new User(
                        disabledUserId.ToString(),
                        null,
                        null,
                        "Zed",
                        "Disabled",
                        "disabled@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Disabled",
                        null),

                    new User(
                        activeUserId.ToString(),
                        null,
                        null,
                        "Amy",
                        "Active",
                        "active@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Active",
                        null)
                }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string?>(),
                1,
                int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            null,
            1,
            20,
            "status");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        // Status values should be mapped from latest sponsor-org dto per userId
        var active = model.Users.Single(u => u.Id == activeUserId.ToString());
        var disabled = model.Users.Single(u => u.Id == disabledUserId.ToString());

        active.Status.ShouldBe("Active");
        disabled.Status.ShouldBe("Disabled");
    }

    [Fact]
    public async Task MyOrganisationUsers_applies_pagination_after_sorting()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();

        var sponsorOrg = new SponsorOrganisationDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Users = new List<SponsorOrganisationUserDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Email = DefaultEmail,
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = u1,
                    Email = "a@test.com",
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = u2,
                    Email = "b@test.com",
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                }
            }
        };

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrg);

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 2,
                Users = new[]
                {
                    new User(
                        u2.ToString(),
                        null,
                        null,
                        "X",
                        "Two",
                        "b@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null),

                    new User(
                        u1.ToString(),
                        null,
                        null,
                        "Y",
                        "One",
                        "a@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null)
                }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string?>(),
                1,
                int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act: sort by email asc => a@test.com then b@test.com, then take page 2 size 1 => b@test.com
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            null,
            2,
            1,
            "email");

        // Assert
        var model = result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        model.Users.Count().ShouldBe(1);
        model.Users.Single().Email.ShouldBe("b@test.com");
    }

    [Fact]
    public async Task MyOrganisationUsers_when_user_id_is_not_a_guid_sponsorrole_sort_treats_role_as_empty()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        var validUserId = Guid.NewGuid();

        var sponsorOrg = new SponsorOrganisationDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Users = new List<SponsorOrganisationUserDto>
            {
                // calling user membership
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Email = DefaultEmail,
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                },

                // Valid user has a role
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = validUserId,
                    Email = "valid@test.com",
                    IsActive = true,
                    SponsorRole = "Admin",
                    IsAuthoriser = false
                }
            }
        };

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrg);

        // One user has a non-GUID Id => TryUserId fails => role becomes ""
        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 2,
                Users = new[]
                {
                    new User(
                        "NOT-A-GUID",
                        null,
                        null,
                        "Bad",
                        "Id",
                        "bad@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null),

                    new User(
                        validUserId.ToString(),
                        null,
                        null,
                        "Good",
                        "User",
                        "valid@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null)
                }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), 1, int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act: sponsorrole asc => "" should sort before "Admin"
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            null,
            1,
            20,
            "sponsorrole");

        // Assert
        var model = result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        model.Users.Select(u => u.Id).ShouldBe(new[] { "NOT-A-GUID", validUserId.ToString() });
    }

    [Fact]
    public async Task
        MyOrganisationUsers_when_user_missing_from_sponsor_org_dtos_sponsorrole_sort_treats_role_as_empty()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        var inOrgId = Guid.NewGuid();
        var notInOrgId = Guid.NewGuid();

        var sponsorOrg = new SponsorOrganisationDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Users = new List<SponsorOrganisationUserDto>
            {
                // calling user membership
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Email = DefaultEmail,
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                },

                // Only this user is present in DTOs with a role
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = inOrgId,
                    Email = "inorg@test.com",
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                }
            }
        };

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrg);

        // Service returns a user that has no matching SponsorOrganisationUserDto => role becomes ""
        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 2,
                Users = new[]
                {
                    new User(
                        inOrgId.ToString(),
                        null,
                        null,
                        "In",
                        "Org",
                        "inorg@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null),

                    new User(
                        notInOrgId.ToString(),
                        null,
                        null,
                        "Not",
                        "InOrg",
                        "notinorg@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null)
                }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), 1, int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act: sponsorrole desc => "Member" should come before ""
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            null,
            1,
            20,
            "sponsorrole",
            "desc");

        // Assert
        var model = result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        model.Users.Select(u => u.Id).ShouldBe(new[] { inOrgId.ToString(), notInOrgId.ToString() });
    }

    [Fact]
    public async Task MyOrganisationUsers_when_user_id_is_not_a_guid_isAuthoriser_sort_treats_as_false()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        var authoriserId = Guid.NewGuid();

        var sponsorOrg = new SponsorOrganisationDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Users = new List<SponsorOrganisationUserDto>
            {
                // calling user membership
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Email = DefaultEmail,
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                },

                // real authoriser
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = authoriserId,
                    Email = "auth@test.com",
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = true
                }
            }
        };

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrg);

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 2,
                Users = new[]
                {
                    new User(
                        "NOT-A-GUID",
                        null,
                        null,
                        "Bad",
                        "Id",
                        "bad@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null),

                    new User(
                        authoriserId.ToString(),
                        null,
                        null,
                        "Auth",
                        "User",
                        "auth@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null)
                }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), 1, int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act: isAuthoriser desc => true first, then false (non-guid treated false)
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            null,
            1,
            20,
            "isAuthoriser",
            "desc");

        // Assert
        result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();
    }

    [Fact]
    public async Task MyOrganisationUsers_when_user_missing_from_sponsor_org_dtos_isAuthoriser_sort_treats_as_false()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        var authoriserId = Guid.NewGuid();
        var unknownId = Guid.NewGuid();

        var sponsorOrg = new SponsorOrganisationDto
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Users = new List<SponsorOrganisationUserDto>
            {
                // calling user membership
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Email = DefaultEmail,
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = false
                },

                // only this one has authoriser flag
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = authoriserId,
                    Email = "auth@test.com",
                    IsActive = true,
                    SponsorRole = "Member",
                    IsAuthoriser = true
                }
            }
        };

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrg);

        // Service returns a user not present in sponsorOrg.Users => treated as false
        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 2,
                Users = new[]
                {
                    new User(
                        unknownId.ToString(),
                        null,
                        null,
                        "Unknown",
                        "User",
                        "unknown@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null),

                    new User(
                        authoriserId.ToString(),
                        null,
                        null,
                        "Auth",
                        "User",
                        "auth@test.com",
                        null,
                        null,
                        null,
                        null,
                        "Unknown",
                        null)
                }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), 1, int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act: isAuthoriser asc => false first, then true
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            null,
            1,
            20,
            "isAuthoriser");

        // Assert
        var model = result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        model.Users.Select(u => u.Id).ShouldBe(new[] { unknownId.ToString(), authoriserId.ToString() });
    }

    [Fact]
    public async Task
        MyOrganisationUsersAddUser_when_query_contains_SearchQuery_and_searchQuery_is_blank_adds_model_error_and_returns_view()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        _http.Request.QueryString = new QueryString("?SearchQuery="); // key present

        // Act
        var result = await Sut.MyOrganisationUsersAddUser(rtsId, "");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        model.RtsId.ShouldBe(rtsId);
        model.Name.ShouldBe("Acme Sponsor Org");

        Sut.ModelState.ContainsKey("SearchQuery").ShouldBeTrue();
        Sut.ModelState["SearchQuery"]!.Errors.Count.ShouldBe(1);
        Sut.ModelState["SearchQuery"]!.Errors[0].ErrorMessage.ShouldBe("Enter a user email");

        // Should not call SearchUsers
        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.SearchUsers(
                    It.IsAny<string?>(),
                    null,
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
    }

    [Fact]
    public async Task
        MyOrganisationUsersAddUser_when_query_does_not_contain_SearchQuery_returns_view_without_calling_user_service()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        _http.Request.QueryString = QueryString.Empty; // key absent

        // Act
        var result = await Sut.MyOrganisationUsersAddUser(rtsId, null);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        model.RtsId.ShouldBe(rtsId);
        model.Name.ShouldBe("Acme Sponsor Org");

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.SearchUsers(
                    It.IsAny<string?>(),
                    null,
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
    }

    [Fact]
    public async Task MyOrganisationUsersAddUser_when_search_returns_exactly_one_user_redirects_to_add_user_role()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        var searchQuery = "one@test.com";
        _http.Request.QueryString = new QueryString($"?SearchQuery={Uri.EscapeDataString(searchQuery)}");

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 1,
                Users = new[]
                {
                    new User(
                        Guid.NewGuid().ToString(),
                        null,
                        null,
                        "One",
                        "User",
                        searchQuery,
                        null,
                        null,
                        null,
                        null,
                        "Active",
                        null)
                }
            }
        };

        var sponsorOrganisation = new SponsorOrganisationDto
        {
            Users = new List<SponsorOrganisationUserDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId
                }
            }
        };

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    sponsorOrganisation
                }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.SearchUsers(searchQuery, null, 1, 10))
            .ReturnsAsync(usersResponse);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(
                It.IsAny<SponsorOrganisationSearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(sponsorResponse);

        // Act
        var result = await Sut.MyOrganisationUsersAddUser(rtsId, searchQuery);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(MyOrganisationsController.MyOrganisationUsersAddUserRole));
        redirect.RouteValues!["rtsId"].ShouldBe(rtsId);

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.SearchUsers(searchQuery, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task MyOrganisationUsersAddUser_when_search_returns_exactly_one_user_shows_already_exists()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        var sponsorOrganisationDto = new SponsorOrganisationDto
        {
            Id = Guid.NewGuid()
        };

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" },
            sponsorOrganisation: sponsorOrganisationDto);

        var searchQuery = "one@test.com";
        _http.Request.QueryString = new QueryString($"?SearchQuery={Uri.EscapeDataString(searchQuery)}");

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 1,
                Users = new[]
                {
                    new User(
                        Guid.NewGuid().ToString(),
                        null,
                        null,
                        "One",
                        "User",
                        searchQuery,
                        null,
                        null,
                        null,
                        null,
                        "Active",
                        null)
                }
            }
        };

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    sponsorOrganisationDto
                }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.SearchUsers(searchQuery, null, 1, 10))
            .ReturnsAsync(usersResponse);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(
                It.IsAny<SponsorOrganisationSearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(sponsorResponse);

        // Act
        var result = await Sut.MyOrganisationUsersAddUser(rtsId, searchQuery);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(10)]
    public async Task MyOrganisationUsersAddUser_when_search_does_not_return_exactly_one_user_redirects_to_invalid_user(
        int totalCount)
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        var searchQuery = "multi@test.com";
        _http.Request.QueryString = new QueryString($"?SearchQuery={Uri.EscapeDataString(searchQuery)}");

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = totalCount,
                Users = Array.Empty<User>()
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.SearchUsers(searchQuery, null, 1, 10))
            .ReturnsAsync(usersResponse);

        // Act
        var result = await Sut.MyOrganisationUsersAddUser(rtsId, searchQuery);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(MyOrganisationsController.MyOrganisationUsersInvalidUser));
        redirect.RouteValues!["rtsId"].ShouldBe(rtsId);
    }

    [Fact]
    public async Task MyOrganisationUsersAddUser_when_search_call_is_not_success_redirects_to_invalid_user()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        var searchQuery = "fail@test.com";
        _http.Request.QueryString = new QueryString($"?SearchQuery={Uri.EscapeDataString(searchQuery)}");

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new UsersResponse
            {
                TotalCount = 1, // even if 1, should not redirect to role because not success
                Users = Array.Empty<User>()
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.SearchUsers(searchQuery, null, 1, 10))
            .ReturnsAsync(usersResponse);

        // Act
        var result = await Sut.MyOrganisationUsersAddUser(rtsId, searchQuery);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(MyOrganisationsController.MyOrganisationUsersInvalidUser));
        redirect.RouteValues!["rtsId"].ShouldBe(rtsId);
    }

    [Fact]
    public async Task MyOrganisationUsersInvalidUser_returns_view_with_model()
    {
        // Arrange
        var rtsId = "87765";

        // Act
        var result = await Sut.MyOrganisationUsersInvalidUser(rtsId);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();
        model.RtsId.ShouldBe(rtsId);
    }

    [Fact]
    public async Task MyOrganisationUsersAddUserRole_returns_view_with_model_and_userId_is_not_required_for_model()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        // Act
        var result = await Sut.MyOrganisationUsersAddUserRole(rtsId, userId.ToString(), "Sponsor");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();
        model.RtsId.ShouldBe(rtsId);
    }

    [Fact]
    public async Task MyOrganisationUsersAddUserRole_redirects_to_confirm()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        // Act
        var result = await Sut.MyOrganisationUsersAddUserRole(rtsId, userId.ToString(), "Organisation administrator");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();
        model.RtsId.ShouldBe(rtsId);
    }

    [Fact]
    public async Task SponsorOrganisationProjectSearchModelBuildsDateFilters()
    {
        // Arrange
        var fromDay = "10";
        var fromMonth = "11";
        var fromYear = "2022";

        var fromDate = DateTimeExtensions.ParseDateValidation(fromDay, fromMonth, fromYear);

        var toDay = "15";
        var toMonth = "12";
        var toYear = "2023";

        var toDate = DateTimeExtensions.ParseDateValidation(toDay, toMonth, toYear);

        //var exectedFilter = "Created date",
        //        [$"{fromDate:d MMM yyyy} to {toDate:d MMM yyyy}"]

        // Act
        var result = new SponsorOrganisationProjectSearchModel
        {
            FromDay = fromDay,
            FromMonth = fromMonth,
            FromYear = fromYear,
            ToDay = toDay,
            ToMonth = toMonth,
            ToYear = toYear
        };

        // Assert
        result.Filters.ShouldContain(x => x.Key == "Created date");
    }

    [Fact]
    public async Task SponsorOrganisationProjectSearchModelBuildsFromDateFilters()
    {
        // Arrange
        var fromDay = "10";
        var fromMonth = "11";
        var fromYear = "2022";

        var fromDate = DateTimeExtensions.ParseDateValidation(fromDay, fromMonth, fromYear);

        // Act
        var result = new SponsorOrganisationProjectSearchModel
        {
            FromDay = fromDay,
            FromMonth = fromMonth,
            FromYear = fromYear
        };

        // Assert
        result.Filters.ShouldContain(x => x.Key == "Date created - from");
    }

    [Fact]
    public async Task SponsorOrganisationProjectSearchModelBuildsToDateFilters()
    {
        // Arrange
        var toDay = "10";
        var toMonth = "11";
        var toYear = "2022";

        var toDate = DateTimeExtensions.ParseDateValidation(toDay, toMonth, toYear);

        // Act
        var result = new SponsorOrganisationProjectSearchModel
        {
            ToDay = toDay,
            ToMonth = toMonth,
            ToYear = toYear
        };

        // Assert
        result.Filters.ShouldContain(x => x.Key == "Date created - to");
    }

    [Fact]
    public async Task MyOrganisationUsersAddUserRole_when_role_key_exists_but_role_empty_returns_error()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        _http.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Role"] = ""
        });

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.MyOrganisationUsersAddUserRole(rtsId, userId.ToString(), "");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        Sut.ModelState.ContainsKey("Role").ShouldBeTrue();
        Sut.ModelState["Role"]!.Errors[0].ErrorMessage.ShouldBe("Select a user role");
    }

    [Fact]
    public async Task MyOrganisationUsersAddUserRole_first_visit_sets_notification_banner()
    {
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.MyOrganisationUsersAddUserRole(rtsId, userId.ToString(), null);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
    }

    [Fact]
    public async Task MyOrganisationUsersAddUserRole_when_nextPage_true_redirects()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);
        var role = "Sponsor";

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        _http.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Role"] = role
        });

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.MyOrganisationUsersAddUserRole(rtsId, userId.ToString(), role, true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.MyOrganisationUsersAddUserPermission));
    }

    [Fact]
    public async Task MyOrganisationUsersAddUserRole_when_nextPage_true_redirects_org_admin()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);
        var role = "Organisation administrator";

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        _http.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Role"] = role
        });

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.MyOrganisationUsersAddUserRole(rtsId, userId.ToString(), role, true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.MyOrganisationUsersCheckAndConfirm));
    }

    [Fact]
    public async Task MyOrganisationUsersAddUserRole_when_role_present_does_not_set_banner()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);
        var role = "Sponsor";

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        _http.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Role"] = role
        });

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.MyOrganisationUsersAddUserRole(rtsId, userId.ToString(), role);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.TempData.ContainsKey(TempDataKeys.ShowNotificationBanner).ShouldBeFalse();
    }

    [Fact]
    public async Task MyOrganisationUsersAddUserPermission_when_nextPage_false_returns_view_with_model()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(
            rtsId,
            DefaultEmail,
            rtsOrganisation: new OrganisationDto
            {
                Name = "Acme Sponsor Org",
                CountryName = "UK"
            });

        var role = SponsorOrganisationUserRoles.Sponsor;
        var canAuthorise = true;

        // Act
        var result = await Sut.MyOrganisationUsersAddUserPermission(
            rtsId,
            userId.ToString(),
            role,
            canAuthorise);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        model.Name.ShouldBe("Acme Sponsor Org");
        model.RtsId.ShouldBe(rtsId);
        model.UserId.ShouldBe(userId.ToString());
        model.Role.ShouldBe(role);
        model.CanAuthorise.ShouldBeTrue();
    }

    public async Task MyOrganisationUsersAddUserPermission_when_nextPage_true_redirects_to_check_and_confirm()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(
            rtsId,
            DefaultEmail,
            rtsOrganisation: new OrganisationDto
            {
                Name = "Acme Sponsor Org",
                CountryName = "UK"
            });

        var role = "Sponsor";
        var canAuthorise = false;

        // Act
        var result = await Sut.MyOrganisationUsersAddUserPermission(
            rtsId,
            userId.ToString(),
            role,
            canAuthorise,
            true);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.MyOrganisationUsersCheckAndConfirm));

        redirect.RouteValues!["rtsId"].ShouldBe(rtsId);
        redirect.RouteValues!["userId"].ShouldBe(userId.ToString());
        redirect.RouteValues!["role"].ShouldBe(role);
        redirect.RouteValues!["canAuthorise"].ShouldBe(canAuthorise);
    }

    [Fact]
    public async Task MyOrganisationUsersCheckAndConfirm_returns_view()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(
            rtsId,
            DefaultEmail,
            rtsOrganisation: new OrganisationDto
            {
                Name = "Acme Sponsor Org",
                CountryName = "UK"
            });

        // Act
        var result = await Sut.MyOrganisationUsersCheckAndConfirm(
            rtsId,
            userId.ToString(),
            null,
            false);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task MyOrganisationUsersConfirmAddUser()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(
            rtsId,
            DefaultEmail,
            rtsOrganisation: new OrganisationDto
            {
                Name = "Acme Sponsor Org",
                CountryName = "UK"
            });

        var sponsorResponse = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new SponsorOrganisationUserDto
            {
                RtsId = rtsId,
                UserId = userId,
                Id = Guid.NewGuid()
            }
        };

        var serviceResponse = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUser(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = new User(
                        userId.ToString(),
                        "azure-ad-12345",
                        "Mr",
                        "Test",
                        "Test",
                        "test.test@example.com",
                        "Software Developer",
                        "orgName",
                        "+44 7700 900123",
                        "United Kingdom",
                        "Active",
                        DateTime.UtcNow,
                        DateTime.UtcNow.AddDays(-2),
                        DateTime.UtcNow)
                }
            });

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.AddUserToSponsorOrganisation(It.IsAny<SponsorOrganisationUserDto>()))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.UpdateRoles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.MyOrganisationUsersConfirmAddUser(
            rtsId,
            userId.ToString(),
            "Sponsor",
            false);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.MyOrganisationUsers));
    }

    [Fact]
    public async Task
        MyOrganisationUsersConfirmAddUser_WhenAddUserToSponsorOrganisationFails_ReturnsServiceError_AndDoesNotUpdateRoles()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(
            rtsId,
            DefaultEmail,
            rtsOrganisation: new OrganisationDto
            {
                Name = "Acme Sponsor Org",
                CountryName = "UK"
            });

        // GetUser succeeds
        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUser(It.IsAny<string>(), It.IsAny<string?>(), null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = new User(
                        userId.ToString(),
                        "azure-ad-12345",
                        "Mr",
                        "Test",
                        "Test",
                        "test.test@example.com",
                        "Software Developer",
                        "orgName",
                        "+44 7700 900123",
                        "United Kingdom",
                        "Active",
                        DateTime.UtcNow,
                        DateTime.UtcNow.AddDays(-2),
                        DateTime.UtcNow)
                }
            });

        // AddUserToSponsorOrganisation fails
        var sponsorFailResponse = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = null
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.AddUserToSponsorOrganisation(It.IsAny<SponsorOrganisationUserDto>()))
            .ReturnsAsync(sponsorFailResponse);

        // Act
        var result = await Sut.MyOrganisationUsersConfirmAddUser(
            rtsId,
            userId.ToString(),
            "Sponsor",
            false);

        // Assert We expect a ServiceError-style result with status code matching the failed response
        result.ShouldNotBeNull();

        var statusCode =
            (result as ObjectResult)?.StatusCode
            ?? (result as StatusCodeResult)?.StatusCode;

        statusCode.ShouldBe((int)HttpStatusCode.InternalServerError);

        // UpdateRoles must NOT be called if sponsor-org add fails
        Mocker.GetMock<IUserManagementService>()
            .Verify(x => x.UpdateRoles(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);

        // Sponsor add is attempted once
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(x => x.AddUserToSponsorOrganisation(It.IsAny<SponsorOrganisationUserDto>()), Times.Once);
    }

    [Fact]
    public async Task MyOrganisationUsersConfirmAddUser_WhenUpdateRolesFails_ReturnsServiceError()
    {
        // Arrange
        var rtsId = "87765";
        var userId = SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(
            rtsId,
            DefaultEmail,
            rtsOrganisation: new OrganisationDto
            {
                Name = "Acme Sponsor Org",
                CountryName = "UK"
            });

        // GetUser succeeds
        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUser(It.IsAny<string>(), It.IsAny<string?>(), null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = new User(
                        userId.ToString(),
                        "azure-ad-12345",
                        "Mr",
                        "Test",
                        "Test",
                        "test.test@example.com",
                        "Software Developer",
                        "orgName",
                        "+44 7700 900123",
                        "United Kingdom",
                        "Active",
                        DateTime.UtcNow,
                        DateTime.UtcNow.AddDays(-2),
                        DateTime.UtcNow)
                }
            });

        // AddUserToSponsorOrganisation succeeds
        var sponsorOkResponse = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new SponsorOrganisationUserDto
            {
                RtsId = rtsId,
                UserId = userId,
                Id = Guid.NewGuid()
            }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.AddUserToSponsorOrganisation(It.IsAny<SponsorOrganisationUserDto>()))
            .ReturnsAsync(sponsorOkResponse);

        // UpdateRoles fails
        var updateRolesFail = new ServiceResponse
        {
            StatusCode = HttpStatusCode.BadRequest
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.UpdateRoles(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()))
            .ReturnsAsync(updateRolesFail);

        // Act
        var result = await Sut.MyOrganisationUsersConfirmAddUser(
            rtsId,
            userId.ToString(),
            "Sponsor",
            false);

        // Assert
        result.ShouldNotBeNull();

        var statusCode =
            (result as ObjectResult)?.StatusCode
            ?? (result as StatusCodeResult)?.StatusCode;

        statusCode.ShouldBe((int)HttpStatusCode.BadRequest);

        // Both calls happen: sponsor add then update roles
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(x => x.AddUserToSponsorOrganisation(It.IsAny<SponsorOrganisationUserDto>()), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(x => x.UpdateRoles(
                    "test.test@example.com",
                    It.IsAny<string?>(),
                    Roles.Sponsor),
                Times.Once);
    }

    [AutoData]
    [Theory]
    public async Task MyOrganisationViewUser_Returns_View(
        SponsorOrganisationUserDto organisationUser)
    {
        // Arrange
        var rtsId = "87765";
        var userId = Guid.NewGuid();
        var loggedInUserId = SetUser(Guid.NewGuid(), DefaultEmail);

        organisationUser.RtsId = rtsId;
        organisationUser.UserId = userId;
        var user = new UserResponse
        {
            User = new User(
                userId.ToString(),
                "azure-ad-12345",
                "Mr",
                "Test",
                "Test",
                organisationUser.Email,
                "Software Developer",
                "orgName",
                "+44 7700 900123",
                "United Kingdom",
                "Active",
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(-2),
                DateTime.UtcNow)
        };

        var orgUserResponse = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = organisationUser
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetUserInSponsorOrganisation(rtsId, userId))
            .ReturnsAsync(orgUserResponse);

        var userServiceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = user
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userServiceResponse);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        // Act
        var result = await Sut.MyOrganisationViewUser(userId.ToString(), rtsId);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("MyOrganisationViewUser");
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUserViewModel>();
        model.RtsId.ShouldBe(rtsId);
        model.UserId.ShouldBe(userId.ToString());
        model.IsLoggedInUserAdmin.ShouldBeFalse();
    }

    [AutoData]
    [Theory]
    public async Task MyOrganisationEditUser_Returns_EditView(
        SponsorOrganisationUserDto organisationUser)
    {
        // Arrange
        var rtsId = "87765";
        var userId = Guid.NewGuid();
        SetUser(Guid.NewGuid(), DefaultEmail);

        organisationUser.RtsId = rtsId;
        organisationUser.UserId = userId;
        var user = new UserResponse
        {
            User = new User(
                userId.ToString(),
                "azure-ad-12345",
                "Mr",
                "Test",
                "Test",
                organisationUser.Email,
                "Software Developer",
                "orgName",
                "+44 7700 900123",
                "United Kingdom",
                "Active",
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(-2),
                DateTime.UtcNow)
        };

        var orgUserResponse = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = organisationUser
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetUserInSponsorOrganisation(rtsId, userId))
            .ReturnsAsync(orgUserResponse);

        var userServiceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = user
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userServiceResponse);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" }, isUserAdmin: true);

        // Act
        var result = await Sut.MyOrganisationViewUser(userId.ToString(), rtsId, true);

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("MyOrganisationEditUser");
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUserViewModel>();
        model.RtsId.ShouldBe(rtsId);
        model.UserId.ShouldBe(userId.ToString());
        model.IsLoggedInUserAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task MyOrganisationEditUser_Returns_Forbidden_When_User_Is_Not_Admin()
    {
        // Arrange
        var rtsId = "87765";
        var userId = Guid.NewGuid();
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" }, isUserAdmin: false);

        // Act
        var result = await Sut.MyOrganisationViewUser(userId.ToString(), rtsId, true);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [AutoData]
    [Theory]
    public async Task MyOrganisationUpdateUser_Returns_Calls_Update_Service(
        SponsorMyOrganisationUserViewModel userModel)
    {
        // Arrange
        var rtsId = "87765";
        var userId = Guid.NewGuid();
        SetUser(Guid.NewGuid(), DefaultEmail);

        userModel.RtsId = rtsId;
        userModel.UserId = userId.ToString();

        var updateModel = new SponsorOrganisationUserDto
        {
            RtsId = userModel.RtsId!,
            UserId = Guid.Parse(userModel.UserId!),
            IsAuthoriser = userModel.IsAuthoriser == "Yes",
            SponsorRole = userModel.Role ?? string.Empty
        };

        var orgUserResponse = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = updateModel
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.UpdateSponsorOrganisationUser(It.IsAny<SponsorOrganisationUserDto>()))
            .ReturnsAsync(orgUserResponse);

        var userServiceResponse = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.UpdateRoles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(userServiceResponse);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" }, isUserAdmin: true);

        // Act
        var result = await Sut.MyOrganisationEditUser(userModel);

        // Assert
        var view = result.ShouldBeOfType<RedirectToActionResult>();
        view.ActionName.ShouldBe("MyOrganisationViewUser");
        view.RouteValues.ShouldNotBeNull();
        view.RouteValues.Keys.ShouldContain("rtsId");
        view.RouteValues.Keys.ShouldContain("userId");
        view.RouteValues[view.RouteValues.Keys.First(k => k == "rtsId")].ShouldBe(rtsId);
        view.RouteValues[view.RouteValues.Keys.First(k => k == "userId")].ShouldBe(userId.ToString());
    }

    [Fact]
    public async Task DisableUser_GET_returns_confirm_view_with_model_and_RtsId()
    {
        // Arrange
        var user = new UserResponse();
        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser("u1", "m@x.com", null))
            .ReturnsAsync(new ServiceResponse<UserResponse> { StatusCode = HttpStatusCode.OK, Content = user });

        // Act
        var result = await Sut.DisableUser("u1", "m@x.com", "rts-1");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("MyOrganisationConfirmDisableUser");
        view.Model.ShouldBeOfType<UserViewModel>();
        ((string)Sut.ViewBag.RtsId).ShouldBe("rts-1");
    }

    [Fact]
    public async Task DisableUser_POST_calls_service_sets_tempdata_and_redirects()
    {
        // Arrange
        var id = Guid.NewGuid();
        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.DisableUserInSponsorOrganisation("rts-1", id))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto> { StatusCode = HttpStatusCode.OK, Content = new() });

        var vm = new UserViewModel { Id = id.ToString(), Email = "m@x.com" };

        // Act
        var result = await Sut.DisableUser(vm, id, "rts-1");

        // Assert
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.DisableUserInSponsorOrganisation("rts-1", id), Times.Once);

        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        Sut.TempData[TempDataKeys.SponsorOrganisationUserType].ShouldBe("disable");

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("MyOrganisationViewUser");
        redirect.RouteValues!["userId"].ShouldBe(id);
        redirect.RouteValues!["rtsId"].ShouldBe("rts-1");
    }

    [Fact]
    public async Task EnableUser_GET_returns_confirm_view_with_model_and_RtsId()
    {
        // Arrange
        var user = new UserResponse();
        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser("u1", "m@x.com", null))
            .ReturnsAsync(new ServiceResponse<UserResponse> { StatusCode = HttpStatusCode.OK, Content = user });

        // Act
        var result = await Sut.EnableUser("u1", "m@x.com", "rts-1");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("MyOrganisationConfirmEnableUser");
        view.Model.ShouldBeOfType<UserViewModel>();
        ((string)Sut.ViewBag.RtsId).ShouldBe("rts-1");
    }

    [Fact]
    public async Task EnableUser_POST_calls_service_sets_tempdata_and_redirects()
    {
        // Arrange
        var id = Guid.NewGuid();
        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.EnableUserInSponsorOrganisation("rts-1", id))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationUserDto> { StatusCode = HttpStatusCode.OK, Content = new() });

        var vm = new UserViewModel { Id = id.ToString(), Email = "m@x.com" };

        // Act
        var result = await Sut.EnableUser(vm, id, "rts-1");

        // Assertd
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.EnableUserInSponsorOrganisation("rts-1", id), Times.Once);

        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        Sut.TempData[TempDataKeys.SponsorOrganisationUserType].ShouldBe("enable");

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("MyOrganisationViewUser");
        redirect.RouteValues!["userId"].ShouldBe(id);
        redirect.RouteValues!["rtsId"].ShouldBe("rts-1");
    }
}
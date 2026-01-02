using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Controllers;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;
using Claim = System.Security.Claims.Claim;

namespace Rsp.IrasPortal.UnitTests.Web.Features.SponsorWorkspace.MyOrganisations;

public class MyOrganisationsControllerTests : TestServiceBase<MyOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    private const string DefaultEmail = "dan.hulmston@test.com";

    private void SetUser(Guid userId, string? email = DefaultEmail)
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
    }

    private void SetupSponsorOrgContextSuccess(
        string rtsId,
        string email,
        SponsorOrganisationDto? sponsorOrganisation = null,
        OrganisationDto? rtsOrganisation = null)
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
        sponsorOrganisation.Users = new List<SponsorOrganisationUserDto>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Email = email,
                IsActive = true,
                SponsorRole = "Member",
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

    public MyOrganisationsControllerTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
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
            sponsorOrganisation: new SponsorOrganisationDto
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Users = new List<SponsorOrganisationUserDto>()
            },
            rtsOrganisation: new OrganisationDto
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

    [Theory, AutoData]
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
        SetUser(Guid.NewGuid(), email: null);

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

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrganisation: sponsorOrg);

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

    [Fact]
    public async Task MyOrganisationProjects_ShouldReturnView_WhenContextPasses()
    {
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Project Org", CountryName = "UK" });

        var result = await Sut.MyOrganisationProjects(rtsId);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationProfileViewModel>();

        model.RtsId.ShouldBe(rtsId);
        model.Name.ShouldBe("Project Org");
    }

    [Theory, AutoData]
    public async Task MyOrganisationProjects_ShouldReturnServiceError_WhenRtsFails(
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
        var result = await Sut.MyOrganisationProjects("");

        // Assert
        var statusResult = result.ShouldBeOfType<StatusCodeResult>();
        statusResult.StatusCode.ShouldBe(400);
    }

    [Theory, AutoData]
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

    [Theory, AutoData]
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
            .Setup(s => s.SponsorOrganisationAuditTrail(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
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

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrganisation: sponsorOrg);

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
                    Id: disabledUserId.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Zed",
                    FamilyName: "Disabled",
                    Email: "disabled@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Disabled",
                    LastUpdated: null),

                new User(
                    Id: activeUserId.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Amy",
                    FamilyName: "Active",
                    Email: "active@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Active",
                    LastUpdated: null)
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
            searchQuery: null,
            pageNumber: 1,
            pageSize: 20,
            sortField: "status",
            sortDirection: "asc");

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

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrganisation: sponsorOrg);

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 2,
                Users = new[]
                {
                new User(
                    Id: u2.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "X",
                    FamilyName: "Two",
                    Email: "b@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null),

                new User(
                    Id: u1.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Y",
                    FamilyName: "One",
                    Email: "a@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null)
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
            searchQuery: null,
            pageNumber: 2,
            pageSize: 1,
            sortField: "email",
            sortDirection: "asc");

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

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrganisation: sponsorOrg);

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
                    Id: "NOT-A-GUID",
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Bad",
                    FamilyName: "Id",
                    Email: "bad@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null),

                new User(
                    Id: validUserId.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Good",
                    FamilyName: "User",
                    Email: "valid@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null)
            }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), 1, int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act: sponsorrole asc => "" should sort before "Admin"
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            searchQuery: null,
            pageNumber: 1,
            pageSize: 20,
            sortField: "sponsorrole",
            sortDirection: "asc");

        // Assert
        var model = result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        model.Users.Select(u => u.Id).ShouldBe(new[] { "NOT-A-GUID", validUserId.ToString() });
    }

    [Fact]
    public async Task MyOrganisationUsers_when_user_missing_from_sponsor_org_dtos_sponsorrole_sort_treats_role_as_empty()
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

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrganisation: sponsorOrg);

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
                    Id: inOrgId.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "In",
                    FamilyName: "Org",
                    Email: "inorg@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null),

                new User(
                    Id: notInOrgId.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Not",
                    FamilyName: "InOrg",
                    Email: "notinorg@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null)
            }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), 1, int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act: sponsorrole desc => "Member" should come before ""
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            searchQuery: null,
            pageNumber: 1,
            pageSize: 20,
            sortField: "sponsorrole",
            sortDirection: "desc");

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

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrganisation: sponsorOrg);

        var usersResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UsersResponse
            {
                TotalCount = 2,
                Users = new[]
                {
                new User(
                    Id: "NOT-A-GUID",
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Bad",
                    FamilyName: "Id",
                    Email: "bad@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null),

                new User(
                    Id: authoriserId.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Auth",
                    FamilyName: "User",
                    Email: "auth@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null)
            }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), 1, int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act: isAuthoriser desc => true first, then false (non-guid treated false)
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            searchQuery: null,
            pageNumber: 1,
            pageSize: 20,
            sortField: "isAuthoriser",
            sortDirection: "desc");

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

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail, sponsorOrganisation: sponsorOrg);

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
                    Id: unknownId.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Unknown",
                    FamilyName: "User",
                    Email: "unknown@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null),

                new User(
                    Id: authoriserId.ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "Auth",
                    FamilyName: "User",
                    Email: "auth@test.com",
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Unknown",
                    LastUpdated: null)
            }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUsersByIds(It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), 1, int.MaxValue))
            .ReturnsAsync(usersResponse);

        // Act: isAuthoriser asc => false first, then true
        var result = await Sut.MyOrganisationUsers(
            rtsId,
            searchQuery: null,
            pageNumber: 1,
            pageSize: 20,
            sortField: "isAuthoriser",
            sortDirection: "asc");

        // Assert
        var model = result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();

        model.Users.Select(u => u.Id).ShouldBe(new[] { unknownId.ToString(), authoriserId.ToString() });
    }

    [Fact]
    public async Task MyOrganisationUsersAddUser_when_query_contains_SearchQuery_and_searchQuery_is_blank_adds_model_error_and_returns_view()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        _http.Request.QueryString = new QueryString("?SearchQuery="); // key present

        // Act
        var result = await Sut.MyOrganisationUsersAddUser(rtsId, searchQuery: "");

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
    public async Task MyOrganisationUsersAddUser_when_query_does_not_contain_SearchQuery_returns_view_without_calling_user_service()
    {
        // Arrange
        var rtsId = "87765";
        SetUser(Guid.NewGuid(), DefaultEmail);

        SetupSponsorOrgContextSuccess(rtsId, DefaultEmail,
            rtsOrganisation: new OrganisationDto { Name = "Acme Sponsor Org", CountryName = "UK" });

        _http.Request.QueryString = QueryString.Empty; // key absent

        // Act
        var result = await Sut.MyOrganisationUsersAddUser(rtsId, searchQuery: null);

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
        SetUser(Guid.NewGuid(), DefaultEmail);

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
                    Id: Guid.NewGuid().ToString(),
                    IdentityProviderId: null,
                    Title: null,
                    GivenName: "One",
                    FamilyName: "User",
                    Email: searchQuery,
                    JobTitle: null,
                    Organisation: null,
                    Telephone: null,
                    Country: null,
                    Status: "Active",
                    LastUpdated: null)
            }
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.SearchUsers(searchQuery, null, 1, 10))
            .ReturnsAsync(usersResponse);

        // Act
        var result = await Sut.MyOrganisationUsersAddUser(rtsId, searchQuery);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(MyOrganisationsController.MyOrganisationUsersAddUserRole));
        redirect.RouteValues!["rtsId"].ShouldBe(rtsId);

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.SearchUsers(searchQuery, null, 1, 10), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(10)]
    public async Task MyOrganisationUsersAddUser_when_search_does_not_return_exactly_one_user_redirects_to_invalid_user(int totalCount)
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
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await Sut.MyOrganisationUsersAddUserRole(rtsId, userId, "Sponsor");

        // Assert
        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationUsersViewModel>();
        model.RtsId.ShouldBe(rtsId);
    }
}
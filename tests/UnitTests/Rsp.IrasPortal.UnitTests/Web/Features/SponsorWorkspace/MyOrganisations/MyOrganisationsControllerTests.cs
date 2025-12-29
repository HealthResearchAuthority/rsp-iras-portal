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

        var result = await Sut.MyOrganisationAuditTrail(rtsId);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<SponsorMyOrganisationProfileViewModel>();

        model.RtsId.ShouldBe(rtsId);
        model.Name.ShouldBe("Audit Org");
    }
}
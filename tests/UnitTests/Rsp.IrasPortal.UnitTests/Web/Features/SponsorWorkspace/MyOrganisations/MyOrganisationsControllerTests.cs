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
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Controllers;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.SponsorWorkspace.MyOrganisations;

public class MyOrganisationsControllerTests : TestServiceBase<MyOrganisationsController>
{
    private readonly DefaultHttpContext _http;

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
    public async Task MyOrganisationProfile_returns_view()
    {
        // Arrange
        SetUser(Guid.NewGuid());

        // Act
        var result = await Sut.MyOrganisationProfile();

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }
}
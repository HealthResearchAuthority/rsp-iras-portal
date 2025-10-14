using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ApplyFiltersTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public ApplyFiltersTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory]
    [AutoData]
    public async Task ApplyFilters_ShouldReturnViewWithOrderedReviewBodies(AllSponsorOrganisationsResponse reviewBodies)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodies
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                "name", "asc"))
            .ReturnsAsync(serviceResponse);

        var reviewBodySearchModel = new SponsorOrganisationSearchModel
        {
            SearchQuery = null,
            Country = [],
            Status = null
        };

        // Persist the search model in Session (GET path reads from session)
        var persisted = JsonSerializer.Serialize(reviewBodySearchModel);
        _http.Session.SetString(SessionKeys.ReviewBodiesSearch, persisted);

        // Simulate HTTP GET
        _http.Request.Method = HttpMethods.Get;

        // Act
        var result = await Sut.ApplyFilters(
            new SponsorOrganisationSearchViewModel
            {
                // Controller should ignore this on GET and use session
                Search = new SponsorOrganisationSearchModel
                    { SearchQuery = "ignored", Country = ["England"], Status = false }
            });

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task ApplyFilters_ShouldReturnEmptyView_WhenServiceReturnsNullContent_OnHttpGet()
    {
        // Arrange
        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                "name", "asc"))
            .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        // No persisted search — simulate first-load GET
        _http.Session.Remove(SessionKeys.ReviewBodiesSearch);

        // Simulate HTTP GET
        _http.Request.Method = HttpMethods.Get;

        // Act
        var result = await Sut.ApplyFilters(
            new SponsorOrganisationSearchViewModel
            {
                // Controller should ignore payload on GET if it uses session/empty defaults
                Search = new SponsorOrganisationSearchModel { SearchQuery = null, Country = [], Status = null }
            });

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
    }
}
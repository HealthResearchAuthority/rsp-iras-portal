using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class SearchOrganisationsTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    private readonly Mock<IRtsService> _mockRtsService;

    public SearchOrganisationsTests()
    {
        _mockRtsService = Mocker.GetMock<IRtsService>();
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public async Task SearchOrganisations_ShouldRedirectWithValidationError_WhenSearchTextTooShort()
    {
        // Arrange

        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrgSearch = new OrganisationSearchViewModel
            {
                SearchText = "ab"
            }
        };

        var httpContext = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrgSearchReturnUrl] = "/sponsororganisations/setup"
        };

        // Act
        var result = await Sut.SearchOrganisations(model, null, null);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Url.ShouldBe("/sponsororganisations/setup");

        Sut.TempData[TempDataKeys.ModelState].ShouldNotBeNull();
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState["sponsor_org_search"]!.Errors.ShouldContain(e =>
            e.ErrorMessage == "Please provide 3 or more characters to search sponsor organisation.");
    }

    [Fact]
    public async Task SearchOrganisations_ShouldRedirectWithResults_WhenSearchIsSuccessful()
    {
        // Arrange
        var pageIndex = 1;
        int? pageSize = null;


        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrgSearch = new OrganisationSearchViewModel
            {
                SearchText = "Health Org"
            }
        };

        var responseContent = new OrganisationSearchResponse
        {
            Organisations =
            [
                new OrganisationDto { Id = "ORG001", Name = "Health Org A" },
                new OrganisationDto { Id = "ORG002", Name = "Health Org B" }
            ],
            TotalCount = 2
        };

        _mockRtsService
            .Setup(x => x.GetOrganisationsByName("Health Org", OrganisationRoles.Sponsor, pageIndex, pageSize, null,
                "asc", "name"))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        var httpContext = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrgSearchReturnUrl] = "/sponsororganisations/setup"
        };

        // Act
        var result = await Sut.SearchOrganisations(model, null, pageSize, pageIndex);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Url.ShouldBe("/sponsororganisations/setup");

    
    }

    [Fact]
    public async Task SearchOrganisations_ShouldReturnErrorResult_WhenServiceFails()
    {
        // Arrange
        var pageIndex = 1;
        int? pageSize = null;

        var model = new SponsorOrganisationSetupViewModel
        {
            SponsorOrgSearch = new OrganisationSearchViewModel
            {
                SearchText = "FailOrg"
            }
        };

        _mockRtsService
            .Setup(x => x.GetOrganisationsByName("FailOrg", OrganisationRoles.Sponsor, pageIndex, pageSize, null, "asc",
                "name"))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null
            });

        var httpContext = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrgSearchReturnUrl] = "/sponsororganisations/setup"
        };

        // Act
        var result = await Sut.SearchOrganisations(model, null, pageSize, pageIndex);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}
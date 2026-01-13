using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ApprovalsControllerTests;

public class SearchOrganisationsTests : TestServiceBase<ApprovalsController>
{
    private readonly Mock<IRtsService> _mockRtsService;

    public SearchOrganisationsTests()
    {
        _mockRtsService = Mocker.GetMock<IRtsService>();
    }

    [Fact]
    public async Task SearchOrganisations_ShouldRedirectWithValidationError_WhenSearchTextTooShort()
    {
        // Arrange
        var model = new ApprovalsSearchViewModel
        {
            Search = new ApprovalsSearchModel
            {
                IrasId = "IRA123",
                FromDay = "01",
                FromMonth = "06",
                FromYear = "2024",
                SponsorOrgSearch = new OrganisationSearchViewModel
                {
                    SearchText = "ab" // too short
                }
            }
        };

        var httpContext = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrgSearchReturnUrl] = "/approvals/search"
        };

        // Act
        var result = await Sut.SearchOrganisations(model, null, null);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Url.ShouldBe("/approvals/search");

        Sut.TempData[TempDataKeys.ModelState].ShouldNotBeNull();
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState["sponsor_org_search"]!.Errors.ShouldContain(e =>
            e.ErrorMessage == "Please provide 3 or more characters to search sponsor organisation.");
    }

    [Fact]
    public async Task SearchOrganisations_ShouldRedirectWithResults_WhenSearchIsSuccessful()
    {
        // Arrange
        int pageIndex = 1;
        int? pageSize = null;

        var model = new ApprovalsSearchViewModel
        {
            Search = new ApprovalsSearchModel
            {
                IrasId = "IRA123",
                FromDay = "01",
                FromMonth = "06",
                FromYear = "2024",
                SponsorOrgSearch = new OrganisationSearchViewModel
                {
                    SearchText = "Health Org"
                }
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
            .Setup(x => x.GetOrganisationsByName("Health Org", OrganisationRoles.Sponsor, pageIndex, pageSize, null, "asc", "name"))
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
            [TempDataKeys.OrgSearchReturnUrl] = "/approvals/search"
        };

        // Act
        var result = await Sut.SearchOrganisations(model, null, pageSize, pageIndex);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Url.ShouldBe("/approvals/search");

        // short-lived flags remain in TempData
        Sut.TempData[TempDataKeys.SponsorOrgSearched].ShouldBe("searched:true");

        // the persisted search model is now in Session (not TempData)
        var json = httpContext.Session.GetString(SessionKeys.ApprovalsSearch);
        json.ShouldNotBeNullOrWhiteSpace();

        var deserialized = JsonSerializer.Deserialize<ApprovalsSearchModel>(json!)!;
        deserialized.IrasId.ShouldBe("IRA123");
        deserialized.FromDate.ShouldBe(new DateTime(2024, 6, 1));
    }

    [Fact]
    public async Task SearchOrganisations_ShouldReturnErrorResult_WhenServiceFails()
    {
        // Arrange
        int pageIndex = 1;
        int? pageSize = null;

        var model = new ApprovalsSearchViewModel
        {
            Search = new ApprovalsSearchModel
            {
                IrasId = "IRA123",
                FromDay = "01",
                FromMonth = "06",
                FromYear = "2024",
                SponsorOrgSearch = new OrganisationSearchViewModel
                {
                    SearchText = "FailOrg"
                }
            }
        };

        _mockRtsService
            .Setup(x => x.GetOrganisationsByName("FailOrg", OrganisationRoles.Sponsor, pageIndex, pageSize, null, "asc", "name"))
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
            [TempDataKeys.OrgSearchReturnUrl] = "/approvals/search"
        };

        // Act
        var result = await Sut.SearchOrganisations(model, null, pageSize, pageIndex);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}
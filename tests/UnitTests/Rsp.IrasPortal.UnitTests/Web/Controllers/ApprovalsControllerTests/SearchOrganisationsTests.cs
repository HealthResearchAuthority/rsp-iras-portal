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

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApprovalsControllerTests;

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
                    SearchText = "ab"
                }
            }
        };

        var httpContext = new DefaultHttpContext();
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrgSearchReturnUrl] = "/approvals/search"
        };

        var result = await Sut.SearchOrganisations(model, null, null);

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

        _mockRtsService.Setup(x => x.GetOrganisationsByName("Health Org", OrganisationRoles.Sponsor, null, null))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        var httpContext = new DefaultHttpContext();
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrgSearchReturnUrl] = "/approvals/search"
        };

        var result = await Sut.SearchOrganisations(model, null, null);

        var redirect = result.ShouldBeOfType<RedirectResult>();
        redirect.Url.ShouldBe("/approvals/search");

        Sut.TempData[TempDataKeys.SponsorOrgSearched].ShouldBe("searched:true");

        var deserialized = JsonSerializer.Deserialize<ApprovalsSearchModel>(
            Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!
        );

        deserialized!.IrasId.ShouldBe("IRA123");
        deserialized.FromDate.ShouldBe(new DateTime(2024, 6, 1));
    }

    [Fact]
    public async Task SearchOrganisations_ShouldReturnErrorResult_WhenServiceFails()
    {
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

        _mockRtsService.Setup(x => x.GetOrganisationsByName("FailOrg", OrganisationRoles.Sponsor, null, null))
            .ReturnsAsync(new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = null
            });

        var httpContext = new DefaultHttpContext();
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrgSearchReturnUrl] = "/approvals/search"
        };

        var result = await Sut.SearchOrganisations(model, null, null);

        result.ShouldBeOfType<ViewResult>();
    }
}
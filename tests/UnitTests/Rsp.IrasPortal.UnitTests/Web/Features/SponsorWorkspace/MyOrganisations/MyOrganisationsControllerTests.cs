using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Controllers;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Controllers;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.SponsorWorkspace.MyOrganisations;

public class MyOrganisationsControllerTests : TestServiceBase<MyOrganisationsController>
{
    private readonly DefaultHttpContext _http;
    private readonly Guid _sponsorOrganisationUserId = Guid.NewGuid();

    public MyOrganisationsControllerTests()
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
    public async Task Authorisations_Returns_View_With_Correct_Model()
    {
        var result = await Sut.MyOrganisations(_sponsorOrganisationUserId);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorMyOrganisationsViewModel>();

        model.ShouldNotBeNull();
        model.SponsorOrganisationUserId.ShouldBe(_sponsorOrganisationUserId);
    }
}
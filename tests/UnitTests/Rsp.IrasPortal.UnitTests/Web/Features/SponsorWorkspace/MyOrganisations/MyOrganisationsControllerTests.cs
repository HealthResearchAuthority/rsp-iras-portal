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

    [Theory]
    [AutoData]
    public void ViewModel_CanHold_List_Of_SponsorMyOrganisationModels(
        Guid sponsorOrganisationUserId,
        SponsorMyOrganisationModel organisation1,
        SponsorMyOrganisationModel organisation2)
    {
        // Arrange
        organisation1.Countries = new List<string> { "UK", "IE" };
        organisation2.Countries = new List<string> { "FR" };

        var organisations = new List<SponsorMyOrganisationModel>
        {
            organisation1,
            organisation2
        };

        var viewModel = new SponsorMyOrganisationsViewModel
        {
            SponsorOrganisationUserId = sponsorOrganisationUserId,
            MyOrganisations = organisations
        };

        // Act
        var result = viewModel.MyOrganisations;

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);

        var first = result.First();
        first.Id.ShouldBe(organisation1.Id);
        first.RtsId.ShouldBe(organisation1.RtsId);
        first.SponsorOrganisationName.ShouldBe(organisation1.SponsorOrganisationName);
        first.Countries.ShouldBe(organisation1.Countries);
        first.Description.ShouldBe(organisation1.Description);
        first.IsActive.ShouldBe(organisation1.IsActive);
        first.CreatedBy.ShouldBe(organisation1.CreatedBy);
        first.CreatedDate.ShouldBe(organisation1.CreatedDate);
        first.UpdatedBy.ShouldBe(organisation1.UpdatedBy);
        first.UpdatedDate.ShouldBe(organisation1.UpdatedDate);
    }
}
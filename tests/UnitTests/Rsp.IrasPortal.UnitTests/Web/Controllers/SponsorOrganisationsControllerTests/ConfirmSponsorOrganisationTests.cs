using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ConfirmSponsorOrganisationTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public ConfirmSponsorOrganisationTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public void ConfirmSponsorOrganisation_ShouldReturnConfirmView_WithProvidedModel()
    {
        // Arrange
        var model = new SponsorOrganisationModel
        {
            SponsorOrganisationName = "Acme Research Ltd",
            Countries = ["England"],
            RtsId = "87765"
        };

        // Act
        var result = Sut.ConfirmSponsorOrganisation(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ConfirmSponsorOrganisation");

        var returnedModel = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationModel>();
        returnedModel.ShouldBe(model);

        // No service calls expected
        Mocker.GetMock<ISponsorOrganisationService>()
            .VerifyNoOtherCalls();
    }
}
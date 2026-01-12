using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ConfirmEnableSponsorOrganisationTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public ConfirmEnableSponsorOrganisationTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory]
    [AutoData]
    public async Task ConfirmEnableSponsorOrganisation_ReturnsToView(
        SponsorOrganisationUserDto sponsorOrganisationUserDto)
    {
        // Arrange
        const string rtsId = "87765";

        var sponsorOrganisationDto = new SponsorOrganisationDto
        {
            RtsId = rtsId,
            IsActive = true
        };

        var serviceResponse = new ServiceResponse<SponsorOrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = sponsorOrganisationDto
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.EnableSponsorOrganisation(rtsId))
            .ReturnsAsync(serviceResponse);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShowNotificationBanner] = true
        };

        // Act
        var result = await Sut.ConfirmEnableSponsorOrganisation(rtsId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("Index");

    }
}
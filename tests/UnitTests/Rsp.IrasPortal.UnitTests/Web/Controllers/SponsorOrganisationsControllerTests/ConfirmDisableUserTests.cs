using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ConfirmDisableUserTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public ConfirmDisableUserTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory]
    [AutoData]
    public async Task ConfirmDisableUser_ReturnsToView(
        SponsorOrganisationUserDto sponsorOrganisationUserDto)
    {
        // Arrange
        const string rtsId = "87765";

        var userGuid = Guid.NewGuid();

        var serviceResponseGetUserInSponsorOrganisation = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = sponsorOrganisationUserDto
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.DisableUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponseGetUserInSponsorOrganisation);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShowNotificationBanner] = true
        };

        // Act
        var result = await Sut.ConfirmDisableUser(rtsId, userGuid);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
    }
}
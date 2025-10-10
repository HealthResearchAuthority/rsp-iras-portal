using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class SaveSponsorOrganisationTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public SaveSponsorOrganisationTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        _http.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "test.user") },
                "mock"));

        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task SaveSponsorOrganisation_ShouldRedirectToIndex_WhenServiceSucceeds()
    {
        // Arrange
        var model = new SponsorOrganisationModel
        {
            SponsorOrganisationName = "Acme Research Ltd",
            Countries = ["England"],
            RtsId = "87765"
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.CreateSponsorOrganisation(It.IsAny<SponsorOrganisationDto>()))
            .ReturnsAsync(new ServiceResponse<SponsorOrganisationDto>
            {
                StatusCode = HttpStatusCode.Created,
                Content = new SponsorOrganisationDto()
            });

        // Act
        var result = await Sut.SaveSponsorOrganisation(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        redirect.ControllerName.ShouldBeNull();

        // TempData notification flag should be set
        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);

        // Verify correct service interaction
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.CreateSponsorOrganisation(It.Is<SponsorOrganisationDto>(dto =>
                dto.SponsorOrganisationName == model.SponsorOrganisationName &&
                dto.RtsId == model.RtsId &&
                dto.Countries.SequenceEqual(model.Countries)
            )), Times.Once);

        // Ensure CreatedBy/CreatedDate are set
        model.CreatedBy.ShouldBe("test.user");
        model.CreatedDate.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public async Task SaveSponsorOrganisation_ShouldReturnServiceError_WhenServiceFails()
    {
        // Arrange
        var model = new SponsorOrganisationModel
        {
            SponsorOrganisationName = "Acme Research Ltd",
            Countries = ["England"],
            RtsId = "87765"
        };

        var errorResponse = new ServiceResponse<SponsorOrganisationDto>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = null
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.CreateSponsorOrganisation(It.IsAny<SponsorOrganisationDto>()))
            .ReturnsAsync(errorResponse);

        // Act
        var result = await Sut.SaveSponsorOrganisation(model);

        // Assert
        // Because `ServiceError()` likely returns an ObjectResult, check accordingly
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);

        // TempData banner should NOT be set on failure
        Sut.TempData.ContainsKey(TempDataKeys.ShowNotificationBanner).ShouldBeFalse();

        // Verify correct service call
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.CreateSponsorOrganisation(It.IsAny<SponsorOrganisationDto>()), Times.Once);
    }
}
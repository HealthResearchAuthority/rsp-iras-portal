using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ViewSponsorOrganisationTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public ViewSponsorOrganisationTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public async Task ViewSponsorOrganisation_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed()
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";
        const string country = "England";

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new() { IsActive = true, CreatedDate = new DateTime(2024, 5, 1) }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName, CountryName = country }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        // Act
        var result = await Sut.ViewSponsorOrganisation(rtsId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationModel>();

        model.RtsId.ShouldBe(rtsId);
        model.SponsorOrganisationName.ShouldBe(orgName);
        model.Countries.ShouldContain(country);
        model.IsActive.ShouldBeTrue();
        model.UpdatedDate.ShouldBe(sponsorResponse.Content.SponsorOrganisations.First().CreatedDate);

        // Verify interactions
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);
    }

    [Fact]
    public async Task ViewSponsorOrganisation_ShouldReturnServiceError_WhenPrimaryServiceFails()
    {
        // Arrange
        const string rtsId = "99999";
        var errorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = null
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(errorResponse);

        // Act
        var result = await Sut.ViewSponsorOrganisation(rtsId);

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);

        // Verify RTS never called
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ViewSponsorOrganisation_ShouldRedirectToIndex_WhenNoSponsorOrgOrSecondCallFails()
    {
        // Arrange
        const string rtsId = "88888";

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new() { IsActive = true, CreatedDate = new DateTime(2024, 5, 1) }
                }
            }
        };

        var rtsFailure = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = null
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(rtsFailure);

        // Act
        var result = await Sut.ViewSponsorOrganisation(rtsId);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");
        redirect.ControllerName.ShouldBeNull();

        // Verify
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);
    }
}
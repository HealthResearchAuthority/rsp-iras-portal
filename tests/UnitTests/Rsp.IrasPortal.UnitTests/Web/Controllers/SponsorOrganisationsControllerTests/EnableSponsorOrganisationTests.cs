using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class EnableSponsorOrganisationTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public EnableSponsorOrganisationTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory]
    [AutoData]
    public async Task EnableSponsorOrganisation_ReturnsToView(
        SponsorOrganisationUserDto sponsorOrganisationUserDto)
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";


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
            Content = new OrganisationDto { Id = rtsId, Name = orgName }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        // Act
        var result = await Sut.EnableSponsorOrganisation(rtsId);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }
}
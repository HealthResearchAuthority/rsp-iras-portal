using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.SponsorOrganisationServiceTests;

public class SponsorOrganisationAuditTrailTests : TestServiceBase<SponsorOrganisationService>
{
    [Fact]
    public async Task GetSponsorOrganisationAuditTrail_ShouldReturnContent()
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<SponsorOrganisationAuditTrailResponse>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == new SponsorOrganisationAuditTrailResponse
            {
                Items = new List<SponsorOrganisationAuditTrailDto>
                {
                    new()
                    {
                        RtsId = "123",
                        DateTimeStamp = DateTime.Today,
                        Description = "12345 created"
                    }
                }
            });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.GetSponsorOrganisationAuditTrail("123", 1, 20, "DateTimeStamp", "desc"))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();

        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.SponsorOrganisationAuditTrail("123", 1, 20, "DateTimeStamp", "desc");

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }
}
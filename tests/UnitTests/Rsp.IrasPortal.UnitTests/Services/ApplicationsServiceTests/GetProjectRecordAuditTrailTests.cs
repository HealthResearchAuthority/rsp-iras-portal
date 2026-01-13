using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ApplicationsServiceTests;

public class GetProjectRecordAuditTrailTests : TestServiceBase<ApplicationsService>
{
    [Theory, AutoData]
    public async Task GetProjectRecordAuditTrail_ShouldReturnAuditTrailResponse_When_OK_Response
    (
        ProjectRecordAuditTrailResponse expectedResponse,
        string projectRecordId
    )
    {
        // Arrange
        Mocker.GetMock<IApplicationsServiceClient>()
        .Setup(s => s.GetProjectRecordAuditTrail(projectRecordId))
            .ReturnsAsync(ApiResponseFactory.Success(expectedResponse));

        // Act
        var result = await Sut.GetProjectRecordAuditTrail(projectRecordId);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(expectedResponse);
    }
}
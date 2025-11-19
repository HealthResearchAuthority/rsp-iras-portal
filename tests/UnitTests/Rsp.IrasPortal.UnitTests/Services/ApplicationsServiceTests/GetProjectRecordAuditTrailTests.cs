using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ApplicationsServiceTests;

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
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationAuditTrail : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetModificationAuditTrail_ShouldReturnAuditTrailResponse_When_OK_Response
    (
        ProjectModificationAuditTrailResponse expectedResponse,
        Guid modificationId
    )
    {
        // Arrange
        Mocker.GetMock<IProjectModificationsServiceClient>()
        .Setup(s => s.GetModificationAuditTrail(modificationId))
            .ReturnsAsync(ApiResponseFactory.Success(expectedResponse));

        // Act
        var result = await Sut.GetModificationAuditTrail(modificationId);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(expectedResponse);
    }
}
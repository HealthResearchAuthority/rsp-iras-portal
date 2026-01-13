using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

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
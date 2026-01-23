using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class CreateModificationDocumentsAuditTrailTests : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task CreateModificationDocumentsAuditTrail_DelegatesToClient_AndReturnsMappedResult(
    List<ModificationDocumentsAuditTrailDto> auditTrailDtos)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<object>(
            httpResponseMessage,
            null,
            new(),
            null);

        var client = Mocker.GetMock<IProjectModificationsServiceClient>();

        client
            .Setup(c => c.CreateModificationDocumentsAuditTrail(auditTrailDtos))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateModificationDocumentsAuditTrail(auditTrailDtos);

        // Assert
        client.Verify(c =>
            c.CreateModificationDocumentsAuditTrail(auditTrailDtos),
            Times.Once);

        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
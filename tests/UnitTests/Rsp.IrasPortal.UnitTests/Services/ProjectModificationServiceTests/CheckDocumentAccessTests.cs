using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class CheckDocumentAccessTests : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task CheckDocumentAccess_DelegatesToClient_AndReturnsMappedResult(
    Guid modificationId)
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
            .Setup(c => c.CheckDocumentAccess(modificationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CheckDocumentAccess(modificationId);

        // Assert
        client.Verify(c =>
            c.CheckDocumentAccess(modificationId),
            Times.Once);

        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
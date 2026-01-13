using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class CreateDocumentModification : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Success_When_Client_Succeeds()
    {
        // Arrange
        var docs = new List<ProjectModificationDocumentRequest>();

        var apiResponse = ApiResponseFactory.Success();

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.CreateModificationDocument(docs))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateDocumentModification(docs);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
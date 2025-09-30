using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

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
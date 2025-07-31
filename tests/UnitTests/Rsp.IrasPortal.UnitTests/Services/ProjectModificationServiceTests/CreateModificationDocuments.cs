using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class CreateModificationDocuments : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task CreateModificationDocuments_DelegatesToClient_AndReturnsMappedResult(
        List<ProjectModificationDocumentRequest> projectModificationDocumentRequest)
    {
        // Arrange

        var apiResponse = Mock.Of<IApiResponse>(i => i.StatusCode == HttpStatusCode.OK);
        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.CreateModificationDocument(projectModificationDocumentRequest))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateDocumentModification(projectModificationDocumentRequest);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
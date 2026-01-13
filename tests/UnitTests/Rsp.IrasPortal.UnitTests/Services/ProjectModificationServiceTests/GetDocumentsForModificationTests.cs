using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class GetDocumentsForModificationTests : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetDocumentsForModification_DelegatesToClient_AndReturnsMappedResult(
    Guid modificationId,
    ProjectOverviewDocumentSearchRequest searchQuery,
    ProjectOverviewDocumentResponse apiResponseContent)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<ProjectOverviewDocumentResponse>(
            httpResponseMessage,
            apiResponseContent,
            new(),
            null);

        var client = Mocker.GetMock<IProjectModificationsServiceClient>();

        client
            .Setup(c => c.GetDocumentsForModification(
                modificationId,
                searchQuery,
                1,
                20,
                "DocumentType",
                "desc"))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetDocumentsForModification(modificationId, searchQuery);

        // Assert
        client.Verify(c =>
            c.GetDocumentsForModification(
                modificationId,
                searchQuery,
                1,
                20,
                "DocumentType",
                "desc"),
            Times.Once);

        result.Content.ShouldBe(apiResponseContent);
    }
}
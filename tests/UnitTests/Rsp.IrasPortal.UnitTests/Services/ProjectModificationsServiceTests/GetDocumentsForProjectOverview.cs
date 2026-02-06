using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetDocumentsForProjectOverview : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_Documents(string projectRecordId)
    {
        // Arrange
        var request = new ProjectOverviewDocumentSearchRequest();
        var response = new ProjectOverviewDocumentResponse
        {
            Documents = [new() { FileName = "doc.pdf" }],
            TotalCount = 1
        };

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetDocumentsForProjectOverview(projectRecordId, request, 1, 20, nameof(ProjectOverviewDocumentDto.DocumentType), "desc"))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.GetDocumentsForProjectOverview(projectRecordId, request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content!.Documents.Count().ShouldBe(1);
    }
}
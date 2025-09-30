using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

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
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

public class GetModificationDocuments : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task GetModificationDocuments_ByChangeId_DelegatesToClient_AndReturnsMappedResult
    (
        Guid projectModificationChangeId,
        string projectRecordId,
        string projectPersonnelId,
        List<ProjectModificationDocumentRequest> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<IEnumerable<ProjectModificationDocumentRequest>>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId, projectPersonnelId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId, projectPersonnelId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId, projectPersonnelId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task GetModificationDocuments_ByChangeIdNoPersonnelId_DelegatesToClient_AndReturnsMappedResult
    (
        Guid projectModificationChangeId,
        string projectRecordId,
        List<ProjectModificationDocumentRequest> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<IEnumerable<ProjectModificationDocumentRequest>>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId);

        // Assert
        respondentServiceClient
            .Verify
            (
                c => c.GetModificationChangesDocuments(projectModificationChangeId, projectRecordId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}
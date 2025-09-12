using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.RespondentServiceTests;

public class ModificationDocumentTests : TestServiceBase<RespondentService>
{
    [Theory, AutoData]
    public async Task GetModificationDocumentDetails_DelegatesToClient_AndReturnsMappedResult
    (
        Guid documentId,
        ProjectModificationDocumentRequest apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<ProjectModificationDocumentRequest>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationDocumentDetails(documentId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationDocumentDetails(documentId);

        // Assert
        respondentServiceClient.Verify(c => c.GetModificationDocumentDetails(documentId), Times.Once);
        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task SaveModificationDocuments_DelegatesToClient_AndReturnsMappedResult
    (
        List<ProjectModificationDocumentRequest> request
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<ProjectModificationDocumentRequest>(httpResponseMessage, null, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.SaveModificationDocuments(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveModificationDocuments(request);

        // Assert
        respondentServiceClient.Verify(c => c.SaveModificationDocuments(request), Times.Once);
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task SaveModificationDocumentAnswers_DelegatesToClient_AndReturnsMappedResult
    (
        List<ProjectModificationDocumentAnswerDto> request
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<ProjectModificationDocumentAnswerDto>(httpResponseMessage, null, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.SaveModificationDocumentAnswer(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.SaveModificationDocumentAnswers(request);

        // Assert
        respondentServiceClient.Verify(c => c.SaveModificationDocumentAnswer(request), Times.Once);
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory, AutoData]
    public async Task GetModificationDocumentAnswers_DelegatesToClient_AndReturnsMappedResult
    (
        Guid documentId,
        List<ProjectModificationDocumentAnswerDto> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>(httpResponseMessage, apiResponseContent, new(), null);

        var respondentServiceClient = Mocker.GetMock<IRespondentServiceClient>();
        respondentServiceClient
            .Setup(c => c.GetModificationDocumentAnswers(documentId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationDocumentAnswers(documentId);

        // Assert
        respondentServiceClient.Verify(c => c.GetModificationDocumentAnswers(documentId), Times.Once);
        result.Content.ShouldBe(apiResponseContent);
    }
}
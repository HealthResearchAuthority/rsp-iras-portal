using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class DeleteDocumentAnswersModification : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task DeleteDocumentAnswersModification_Should_Return_Success_Response_When_Client_Returns_Success(
        List<ProjectModificationDocumentRequest> deleteRequest,
        ProjectOverviewDocumentResponse modificationsResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<ProjectOverviewDocumentResponse>(
            new HttpResponseMessage(HttpStatusCode.OK),
            modificationsResponse,
            new());

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.DeleteDocumentAnswers(deleteRequest))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteDocumentAnswersModification(deleteRequest);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        projectModificationsServiceClient.Verify(c => c.DeleteDocumentAnswers(deleteRequest), Times.Once);
    }

    [Theory, AutoData]
    public async Task DeleteDocumentAnswersModification_Should_Return_Failure_Response_When_Client_Returns_Failure(
        List<ProjectModificationDocumentRequest> deleteRequest,
        ProjectOverviewDocumentResponse modificationsResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<ProjectOverviewDocumentResponse>(
            new HttpResponseMessage(HttpStatusCode.BadRequest),
            modificationsResponse,
            new());

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.DeleteDocumentAnswers(deleteRequest))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteDocumentAnswersModification(deleteRequest);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        projectModificationsServiceClient.Verify(c => c.DeleteDocumentAnswers(deleteRequest), Times.Once);
    }
}
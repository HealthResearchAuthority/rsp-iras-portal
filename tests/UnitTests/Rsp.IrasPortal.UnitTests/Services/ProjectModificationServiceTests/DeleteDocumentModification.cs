using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class DeleteDocumentModification : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task DeleteDocumentModification_Should_Return_Success_Response_When_Client_Returns_Success(
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
            .Setup(c => c.DeleteDocuments(deleteRequest))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteDocumentModification(deleteRequest);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        projectModificationsServiceClient.Verify(c => c.DeleteDocuments(deleteRequest), Times.Once);
    }

    [Theory, AutoData]
    public async Task DeleteDocumentModification_Should_Return_Failure_Response_When_Client_Returns_Failure(
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
            .Setup(c => c.DeleteDocuments(deleteRequest))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteDocumentModification(deleteRequest);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        projectModificationsServiceClient.Verify(c => c.DeleteDocuments(deleteRequest), Times.Once);
    }
}
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class GetModifications : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetModifications_DelegatesToClient_AndReturnsMappedResult
    (
        string projectRecordId,
        List<ProjectModificationResponse> apiResponseContent
    )
    {
        // Arrange

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var apiResponse = new ApiResponse<IEnumerable<ProjectModificationResponse>>(httpResponseMessage, apiResponseContent, new(), null);

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetModifications(projectRecordId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModifications(projectRecordId);

        // Assert
        projectModificationsServiceClient
            .Verify
            (
                c => c.GetModifications(projectRecordId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task GetModificationsByStatus_DelegatesToClient_AndReturnsMappedResult
    (
        string projectRecordId,
        string status,
        List<ProjectModificationResponse> apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var apiResponse = new ApiResponse<IEnumerable<ProjectModificationResponse>>(httpResponseMessage, apiResponseContent, new(), null);

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetModificationsByStatus(projectRecordId, status))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationsByStatus(projectRecordId, status);

        // Assert
        projectModificationsServiceClient
            .Verify
            (
                c => c.GetModificationsByStatus(projectRecordId, status),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }

    [Theory, AutoData]
    public async Task GetModifications_Should_Return_Success_Response_When_Client_Returns_Success(
        ModificationSearchRequest searchQuery,
        GetModificationsResponse modificationsResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<GetModificationsResponse>(
            new HttpResponseMessage(HttpStatusCode.OK),
            modificationsResponse,
            new());

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetModifications(searchQuery, 1, 20, "ModificationId", "desc"))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModifications(searchQuery);

        // Assert
        result.ShouldBeOfType<ServiceResponse<GetModificationsResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(modificationsResponse);

        // Verify
        projectModificationsServiceClient.Verify(c => c.GetModifications(searchQuery, 1, 20, "ModificationId", "desc"), Times.Once());
    }
}
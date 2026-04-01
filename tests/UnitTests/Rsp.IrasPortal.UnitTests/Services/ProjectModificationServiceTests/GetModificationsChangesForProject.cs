using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class GetModificationsChangesForProject
    : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetModificationsChangesForProject_Should_Return_Success_Response_When_Client_Returns_Success(
        string projectRecordId,
        IEnumerable<ProjectModificationChangeResponse> mockChangesResponse)
    {
        // Arrange
        var apiResponse = new ApiResponse<IEnumerable<ProjectModificationChangeResponse>>(
            new HttpResponseMessage(HttpStatusCode.OK),
            mockChangesResponse,
            new());

        var projectModificationsServiceClient =
            Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetModificationsChangesForProject(projectRecordId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationsChangesForProject(projectRecordId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<IEnumerable<ProjectModificationChangeResponse>>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(mockChangesResponse);

        projectModificationsServiceClient.Verify(
            c => c.GetModificationsChangesForProject(projectRecordId),
            Times.Once);
    }
}
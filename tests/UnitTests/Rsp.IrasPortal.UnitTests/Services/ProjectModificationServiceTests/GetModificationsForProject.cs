using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class GetModificationsForProject : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetModificationsForProject_Should_Return_Success_Response_When_Client_Returns_Success(
        string projectRecordId,
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
            .Setup(c => c.GetModificationsForProject(projectRecordId, searchQuery, 1, 20, "ModificationId", "desc"))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationsForProject(projectRecordId, searchQuery);

        // Assert
        result.ShouldBeOfType<ServiceResponse<GetModificationsResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(modificationsResponse);

        // Verify
        projectModificationsServiceClient.Verify(c => c.GetModificationsForProject(projectRecordId, searchQuery, 1, 20, "ModificationId", "desc"), Times.Once());
    }
}
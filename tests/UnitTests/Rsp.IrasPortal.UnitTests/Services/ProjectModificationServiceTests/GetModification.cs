using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class GetModification : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetModification_DelegatesToClient_AndReturnsMappedResult
    (
        Guid projectModificationId,
        ProjectModificationResponse apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var apiResponse = new ApiResponse<ProjectModificationResponse>(httpResponseMessage, apiResponseContent, new(), null);

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModification("PR1", projectModificationId);

        // Assert
        projectModificationsServiceClient
            .Verify
            (
                c => c.GetModification("PR1", projectModificationId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}
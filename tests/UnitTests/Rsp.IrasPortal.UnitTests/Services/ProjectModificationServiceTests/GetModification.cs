using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class GetModification : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetModification_DelegatesToClient_AndReturnsMappedResult
    (
        string projectRecordId,
        Guid projectModificationId,
        ProjectModificationResponse apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var apiResponse = new ApiResponse<ProjectModificationResponse>(httpResponseMessage, apiResponseContent, new(), null);

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetModification(projectRecordId, projectModificationId))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModification(projectRecordId, projectModificationId);

        // Assert
        projectModificationsServiceClient
            .Verify
            (
                c => c.GetModification(projectRecordId, projectModificationId),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}
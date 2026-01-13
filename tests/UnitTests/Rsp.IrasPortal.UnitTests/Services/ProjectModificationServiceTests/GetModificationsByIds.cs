using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationServiceTests;

public class GetModificationsByIds : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetModificationsByIds_DelegatesToClient_AndReturnsMappedResult
    (
        List<string> ids,
        GetModificationsResponse apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var apiResponse = new ApiResponse<GetModificationsResponse>(httpResponseMessage, apiResponseContent, new(), null);

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetModificationsByIds(ids))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetModificationsByIds(ids);

        // Assert
        projectModificationsServiceClient
            .Verify
            (
                c => c.GetModificationsByIds(ids),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}
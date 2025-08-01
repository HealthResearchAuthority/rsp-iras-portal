using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class GetAreaOfChanges : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task GetAreaOfChanges_DelegatesToClient_AndReturnsMappedResult(
        List<GetAreaOfChangesResponse> apiResponseContent)
    {
        // Arrange

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var apiResponse = new ApiResponse<IEnumerable<GetAreaOfChangesResponse>>(httpResponseMessage, apiResponseContent, new(), null);

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.GetAreaOfChanges())
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetAreaOfChanges();

        // Assert
        projectModificationsServiceClient
            .Verify
            (
                c => c.GetAreaOfChanges(),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class CreateModification : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task CreateModification_DelegatesToClient_AndReturnsMappedResult
    (
        ProjectModificationRequest request,
        ProjectModificationResponse apiResponseContent
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var apiResponse = new ApiResponse<ProjectModificationResponse>(httpResponseMessage, apiResponseContent, new(), null);

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.CreateModification(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateModification(request);

        // Assert
        projectModificationsServiceClient
            .Verify
            (
                c => c.CreateModification(request),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}
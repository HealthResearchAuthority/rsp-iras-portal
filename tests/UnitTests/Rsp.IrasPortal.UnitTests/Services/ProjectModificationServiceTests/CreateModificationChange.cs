using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class CreateModificationChange : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task CreateModificationChange_DelegatesToClient_AndReturnsMappedResult
     (
         ProjectModificationChangeRequest request,
         ProjectModificationChangeResponse apiResponseContent
     )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        var apiResponse = new ApiResponse<ProjectModificationChangeResponse>(httpResponseMessage, apiResponseContent, new(), null);

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.CreateModificationChange(request))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateModificationChange(request);

        // Assert
        projectModificationsServiceClient
            .Verify
            (
                c => c.CreateModificationChange(request),
                Times.Once
            );

        result.Content.ShouldBe(apiResponseContent);
    }
}
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class CreateModification : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_Created_Modification(ProjectModificationRequest request, ProjectModificationResponse response)
    {
        // Arrange
        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.CreateModification(request))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.CreateModification(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }
}
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class CreateModificationChange : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_Created_Change(ProjectModificationChangeRequest request, ProjectModificationChangeResponse response)
    {
        // Arrange
        response.Status = "Draft";

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.CreateModificationChange(request))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.CreateModificationChange(request);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.Status.ShouldBe("Draft");
    }
}
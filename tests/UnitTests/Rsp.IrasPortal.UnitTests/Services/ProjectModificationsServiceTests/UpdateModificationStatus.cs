using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class UpdateModificationStatus : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Success_On_200()
    {
        // Arrange
        var id = Guid.NewGuid();

        var apiResponse = ApiResponseFactory.Success();

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.UpdateModificationStatus(id, "Draft"))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.UpdateModificationStatus(id, "Draft");

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
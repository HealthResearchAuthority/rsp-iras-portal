using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class RemoveModificationChange : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Success_On_200()
    {
        // Arrange
        var id = Guid.NewGuid();

        var apiResponse = ApiResponseFactory.Success();

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.RemoveModificationChange(id))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.RemoveModificationChange(id);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
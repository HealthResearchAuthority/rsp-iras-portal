using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModification : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_ServiceResponse_From_Client(Guid modificationId, ProjectModificationResponse response)
    {
        // Arrange
        response.Id = modificationId;

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModification(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.GetModification("PR1", modificationId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.Id.ShouldBe(modificationId);
    }
}
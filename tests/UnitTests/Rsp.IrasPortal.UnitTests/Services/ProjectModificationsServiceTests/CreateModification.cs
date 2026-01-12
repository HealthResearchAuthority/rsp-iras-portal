using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

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
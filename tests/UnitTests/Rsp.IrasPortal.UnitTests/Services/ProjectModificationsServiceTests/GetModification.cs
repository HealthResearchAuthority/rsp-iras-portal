using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModification : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_ServiceResponse_From_Client(string projectRecordId, Guid modificationId, ProjectModificationResponse response)
    {
        // Arrange
        response.Id = modificationId;

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModification(projectRecordId, modificationId))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.GetModification(projectRecordId, modificationId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.Id.ShouldBe(modificationId);
    }
}
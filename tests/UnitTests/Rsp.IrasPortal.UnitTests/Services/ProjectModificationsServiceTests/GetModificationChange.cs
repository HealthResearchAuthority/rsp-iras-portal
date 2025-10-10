using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationChange : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_Change_From_Client(ProjectModificationChangeResponse response)
    {
        // Arrange
        var changeId = Guid.NewGuid();
        response.Status = ModificationStatus.ModificationRecordStarted;
        response.Id = changeId;

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModificationChange(changeId))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.GetModificationChange(changeId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.Id.ShouldBe(changeId);
    }
}
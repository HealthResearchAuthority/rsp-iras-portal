using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationChange : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_Change_From_Client(ProjectModificationChangeResponse response)
    {
        // Arrange
        var changeId = Guid.NewGuid();
        response.Status = ModificationStatus.InDraft;
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
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

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
            .Setup(c => c.UpdateModificationStatus(It.IsAny<UpdateModificationStatusRequest>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.UpdateModificationStatus(
            new UpdateModificationStatusRequest
            {
                ProjectRecordId = "PR-1",
                ModificationId = id,
                Status = ModificationStatus.InDraft
            });

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
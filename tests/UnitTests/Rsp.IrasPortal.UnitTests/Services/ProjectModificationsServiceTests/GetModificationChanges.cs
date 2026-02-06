using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationChanges : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Changes_From_Client()
    {
        // Arrange
        var id = Guid.NewGuid();
        var list = new List<ProjectModificationChangeResponse>
        {
            new() { Id = Guid.NewGuid(), Status = ModificationStatus.InDraft }
        };

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModificationChanges(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(ApiResponseFactory.Success<IEnumerable<ProjectModificationChangeResponse>>(list));

        // Act
        var result = await Sut.GetModificationChanges("PR-1", id);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.Count().ShouldBe(1);
    }
}
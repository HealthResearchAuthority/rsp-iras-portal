using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationChanges : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Changes_From_Client()
    {
        // Arrange
        var id = Guid.NewGuid();
        var list = new List<ProjectModificationChangeResponse>
        {
            new() { Id = Guid.NewGuid(), Status = ModificationStatus.Draft }
        };

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModificationChanges(id))
            .ReturnsAsync(ApiResponseFactory.Success<IEnumerable<ProjectModificationChangeResponse>>(list));

        // Act
        var result = await Sut.GetModificationChanges(id);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.Count().ShouldBe(1);
    }
}
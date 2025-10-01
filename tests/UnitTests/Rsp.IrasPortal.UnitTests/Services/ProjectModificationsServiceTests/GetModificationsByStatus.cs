using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationsByStatus : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_List_From_Client(string projectRecordId, string status)
    {
        // Arrange
        var list = new List<Application.DTOs.Responses.ProjectModificationResponse>
        {
            new() { Id = Guid.NewGuid(), Status = status }
        };

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModificationsByStatus(projectRecordId, status))
            .ReturnsAsync(ApiResponseFactory.Success<IEnumerable<Application.DTOs.Responses.ProjectModificationResponse>>(list));

        // Act
        var result = await Sut.GetModificationsByStatus(projectRecordId, status);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.First().Status.ShouldBe(status);
    }
}
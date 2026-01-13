using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationsByStatus : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_List_From_Client(string projectRecordId, string status)
    {
        // Arrange
        var list = new List<ProjectModificationResponse>
        {
            new() { Id = Guid.NewGuid(), Status = status }
        };

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModificationsByStatus(projectRecordId, status))
            .ReturnsAsync(ApiResponseFactory.Success<IEnumerable<ProjectModificationResponse>>(list));

        // Act
        var result = await Sut.GetModificationsByStatus(projectRecordId, status);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.First().Status.ShouldBe(status);
    }
}
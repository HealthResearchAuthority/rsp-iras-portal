using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModifications : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task Returns_List_From_Client(string projectRecordId)
    {
        // Arrange
        var list = new List<ProjectModificationResponse>
        {
            new() { Id = Guid.NewGuid() }
        };

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModifications(projectRecordId))
            .ReturnsAsync(ApiResponseFactory.Success<IEnumerable<ProjectModificationResponse>>(list));

        // Act
        var result = await Sut.GetModifications(projectRecordId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content!.Count().ShouldBe(1);
    }
}
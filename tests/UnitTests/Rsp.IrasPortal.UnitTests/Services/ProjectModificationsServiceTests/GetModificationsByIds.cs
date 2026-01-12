using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class GetModificationsByIds : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Paged_Response()
    {
        // Arrange
        var ids = new List<string> { Guid.NewGuid().ToString() };
        var response = new GetModificationsResponse
        {
            Modifications = [],
            TotalCount = 0
        };

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.GetModificationsByIds(ids))
            .ReturnsAsync(ApiResponseFactory.Success(response));

        // Act
        var result = await Sut.GetModificationsByIds(ids);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
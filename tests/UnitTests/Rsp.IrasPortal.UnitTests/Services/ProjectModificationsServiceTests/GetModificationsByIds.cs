using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

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
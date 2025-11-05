using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.IrasPortal.UnitTests.TestHelpers;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationsServiceTests;

public class AssignModificationsToReviewer : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Success_On_200()
    {
        // Arrange
        var ids = new List<string> { Guid.NewGuid().ToString() };
        var reviewer = Guid.NewGuid().ToString();
        var reviewerEmail = Guid.NewGuid().ToString();
        var apiResponse = ApiResponseFactory.Success();

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.AssignModificationsToReviewer(ids, reviewer, reviewerEmail))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.AssignModificationsToReviewer(ids, reviewer, reviewerEmail);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
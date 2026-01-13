using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests.TestHelpers;

namespace Rsp.Portal.UnitTests.Services.ProjectModificationsServiceTests;

public class AssignModificationsToReviewer : TestServiceBase<ProjectModificationsService>
{
    [Fact]
    public async Task Returns_Success_On_200()
    {
        // Arrange
        var ids = new List<string> { Guid.NewGuid().ToString() };
        var reviewer = Guid.NewGuid().ToString();
        var reviewerEmail = Guid.NewGuid().ToString();
        var reviewerName = "Test Test";
        var apiResponse = ApiResponseFactory.Success();

        Mocker
            .GetMock<IProjectModificationsServiceClient>()
            .Setup(c => c.AssignModificationsToReviewer(ids, reviewer, reviewerEmail, reviewerName))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.AssignModificationsToReviewer(ids, reviewer, reviewerEmail, reviewerName);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
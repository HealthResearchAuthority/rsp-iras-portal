using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectModificationServiceTests;

public class AssignModificationsToReviewer : TestServiceBase<ProjectModificationsService>
{
    [Theory, AutoData]
    public async Task AssignModificationsToReviewer_DelegatesToClient_AndReturnsMappedResult
    (
        List<string> modificationIds,
        string reviewerId,
        string reviewerEmail
    )
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        // Mock IApiResponse
        var mockApiResponse = new Mock<IApiResponse>();
        mockApiResponse.SetupGet(r => r.IsSuccessStatusCode).Returns(true);
        mockApiResponse.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.OK);

        var projectModificationsServiceClient = Mocker.GetMock<IProjectModificationsServiceClient>();

        projectModificationsServiceClient
            .Setup(c => c.AssignModificationsToReviewer(modificationIds, reviewerId, reviewerEmail))
            .ReturnsAsync(mockApiResponse.Object);

        // Act
        var result = await Sut.AssignModificationsToReviewer(modificationIds, reviewerId, reviewerEmail);

        // Assert
        projectModificationsServiceClient
            .Verify
            (
                c => c.AssignModificationsToReviewer(modificationIds, reviewerId, reviewerEmail),
                Times.Once
            );

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.IsSuccessStatusCode.ShouldBeTrue();
    }
}
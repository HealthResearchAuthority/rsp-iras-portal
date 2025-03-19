using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

public class GetReviewBodiesTests : TestServiceBase<ReviewBodyService>
{
    [Theory, AutoData]
    public async Task GetReviewBodies_Should_Return_Failure_Response_When_Client_Returns_Failure()
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<IEnumerable<ReviewBodyDto>>>(
            apiResponse => !apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.BadRequest);

        var client = new Mock<IReviewBodyServiceClient>();
        client.Setup(c => c.GetAllReviewBodies())
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.GetAllReviewBodies();

        // Assert
        result.ShouldBeOfType<ServiceResponse<IEnumerable<ReviewBodyDto>>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        client.Verify(c => c.GetAllReviewBodies(), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetReviewBodies_Should_Return_Success_Response_When_Client_Returns_Success(
        List<ReviewBodyDto> reviewBodies)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<IEnumerable<ReviewBodyDto>>>(
            apiResponse => apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.OK &&
                           apiResponse.Content == reviewBodies);

        var client = new Mock<IReviewBodyServiceClient>();
        client.Setup(c => c.GetAllReviewBodies())
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.GetAllReviewBodies();

        // Assert
        result.ShouldBeOfType<ServiceResponse<IEnumerable<ReviewBodyDto>>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBeEquivalentTo(reviewBodies);

        // Verify
        client.Verify(c => c.GetAllReviewBodies(), Times.Once());
    }
}
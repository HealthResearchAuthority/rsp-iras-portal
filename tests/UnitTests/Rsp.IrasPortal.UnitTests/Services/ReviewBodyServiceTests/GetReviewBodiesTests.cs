using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
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
        var apiResponse = Mock.Of<IApiResponse<AllReviewBodiesResponse>>(
            apiResponse => !apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.BadRequest);

        var client = new Mock<IReviewBodyServiceClient>();
        client.Setup(c => c.GetAllReviewBodies(1, 100, null))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.GetAllReviewBodies(1, 100, null);

        // Assert
        result.ShouldBeOfType<ServiceResponse<AllReviewBodiesResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        client.Verify(c => c.GetAllReviewBodies(1, 100, null), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetReviewBodies_Should_Return_Success_Response_When_Client_Returns_Success(
        List<ReviewBodyDto> reviewBodies)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<AllReviewBodiesResponse>>(
            apiResponse => apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.OK &&
                           apiResponse.Content == new AllReviewBodiesResponse { ReviewBodies = reviewBodies });

        var client = new Mock<IReviewBodyServiceClient>();
        client.Setup(c => c.GetAllReviewBodies(1, 100, null))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.GetAllReviewBodies(1, 100, null);

        // Assert
        result.ShouldBeOfType<ServiceResponse<AllReviewBodiesResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content!.ReviewBodies.ShouldBeEquivalentTo(reviewBodies);

        // Verify
        client.Verify(c => c.GetAllReviewBodies(1, 100, null), Times.Once());
    }
}
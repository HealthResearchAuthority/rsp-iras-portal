using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

public class GetUserReviewBodiesTests : TestServiceBase<ReviewBodyService>
{
    [Theory]
    [AutoData]
    public async Task GetUserReviewBodies_Should_Return_Failure_Response_When_Client_Returns_Failure(Guid userId)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<List<ReviewBodyUserDto>>>(r => !r.IsSuccessStatusCode &&
                                                                              r.StatusCode == HttpStatusCode.BadRequest
        );

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client
            .Setup(c => c.GetUserReviewBodies(userId))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.GetUserReviewBodies(userId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<List<ReviewBodyUserDto>>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        client.Verify(c => c.GetUserReviewBodies(userId), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task GetUserReviewBodies_Should_Return_Success_Response_With_Content(Guid userId,
        List<ReviewBodyUserDto> expected)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<List<ReviewBodyUserDto>>>(r => r.IsSuccessStatusCode &&
                                                                              r.StatusCode == HttpStatusCode.OK &&
                                                                              r.Content == expected
        );

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client
            .Setup(c => c.GetUserReviewBodies(userId))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.GetUserReviewBodies(userId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<List<ReviewBodyUserDto>>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBeSameAs(expected);

        // Verify
        client.Verify(c => c.GetUserReviewBodies(userId), Times.Once);
    }
}
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

public class UpdateReviewBodyTests : TestServiceBase<ReviewBodyService>
{
    [Theory, AutoData]
    public async Task UpdateReviewBody_Should_Return_Failure_Response_When_Client_Returns_Failure(
        ReviewBodyDto reviewBodyDto)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                !apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.BadRequest
        );

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client
            .Setup(c => c.UpdateReviewBody(reviewBodyDto))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.UpdateReviewBody(reviewBodyDto);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        client.Verify(c => c.UpdateReviewBody(reviewBodyDto), Times.Once());
    }

    [Theory, AutoData]
    public async Task UpdateReviewBody_Should_Return_Success_Response_When_Client_Returns_Success(
        ReviewBodyDto reviewBodyDto)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client
            .Setup(c => c.UpdateReviewBody(reviewBodyDto))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.UpdateReviewBody(reviewBodyDto);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.UpdateReviewBody(reviewBodyDto), Times.Once());
    }
}
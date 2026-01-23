using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ReviewBodyServiceTests;

public class UpdateReviewBodyTests : TestServiceBase<ReviewBodyService>
{
    [Theory, AutoData]
    public async Task UpdateReviewBody_Should_Return_Failure_Response_When_Client_Returns_Failure(
        ReviewBodyDto reviewBodyDto)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyDto>>
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
        result.ShouldBeOfType<ServiceResponse<ReviewBodyDto>>();
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
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyDto>>
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
        result.ShouldBeOfType<ServiceResponse<ReviewBodyDto>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.UpdateReviewBody(reviewBodyDto), Times.Once());
    }
}
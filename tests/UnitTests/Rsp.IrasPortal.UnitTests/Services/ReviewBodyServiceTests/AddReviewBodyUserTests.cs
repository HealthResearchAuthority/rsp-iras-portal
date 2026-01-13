using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ReviewBodyServiceTests;

public class AddReviewBodyUserTests : TestServiceBase<ReviewBodyService>
{
    [Theory]
    [AutoData]
    public async Task AddReviewBodyUser_Should_Return_Failure_Response_When_Client_Returns_Failure(
        ReviewBodyUserDto reviewBodyUserDto)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyUserDto>>
        (
            apiResponse =>
                !apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.BadRequest
        );

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client
            .Setup(c => c.AddUserToReviewBody(reviewBodyUserDto))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.AddUserToReviewBody(reviewBodyUserDto);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyUserDto>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        client.Verify(c => c.AddUserToReviewBody(reviewBodyUserDto), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task AddReviewBodyUser_Should_Return_Success_Response_When_Client_Returns_Success(
        ReviewBodyUserDto reviewBodyUserDto)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyUserDto>>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client
            .Setup(c => c.AddUserToReviewBody(reviewBodyUserDto))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.AddUserToReviewBody(reviewBodyUserDto);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyUserDto>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.AddUserToReviewBody(reviewBodyUserDto), Times.Once());
    }
}
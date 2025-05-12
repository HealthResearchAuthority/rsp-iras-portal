using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

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
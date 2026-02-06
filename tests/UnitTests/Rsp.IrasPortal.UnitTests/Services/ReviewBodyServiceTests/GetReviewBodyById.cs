using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ReviewBodyServiceTests;

public class GetReviewBodiesByIdTests : TestServiceBase<ReviewBodyService>
{
    [Theory, AutoData]
    public async Task GetReviewBodiesById_Should_Return_Failure_Response_When_Client_Returns_Failure(Guid id)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyDto>>(
            apiResponse => !apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.NotFound);

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client.Setup(c => c.GetReviewBodyById(id))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.GetReviewBodyById(id);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyDto>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        client.Verify(c => c.GetReviewBodyById(id), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetReviewBodiesById_Should_Return_Success_Response_When_Client_Returns_Success(Guid id,
        ReviewBodyDto reviewBody)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyDto>>(
            apiResponse => apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.OK &&
                           apiResponse.Content == reviewBody);

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client.Setup(c => c.GetReviewBodyById(id))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.GetReviewBodyById(id);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyDto>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBeEquivalentTo(reviewBody);

        // Verify
        client.Verify(c => c.GetReviewBodyById(id), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetReviewBodiesById_Should_Return_BadRequest_When_Invalid_Id_Is_Provided()
    {
        // Arrange
        var invalidId = Guid.Empty;
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyDto>>(
            apiResponse => !apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.BadRequest);

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client.Setup(c => c.GetReviewBodyById(invalidId))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.GetReviewBodyById(invalidId);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyDto>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        client.Verify(c => c.GetReviewBodyById(invalidId), Times.Once());
    }
}
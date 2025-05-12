using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

public class EnableReviewBodyTests : TestServiceBase<ReviewBodyService>
{
    [Theory]
    [AutoData]
    public async Task EnableReviewBody_Should_Return_Success(
        ReviewBodyDto reviewBodyDto)
    {
        // Arrange
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyDto>>(
            apiResponse => apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.OK &&
                           apiResponse.Content == reviewBodyDto);

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client
            .Setup(c => c.EnableReviewBody(It.IsAny<Guid>()))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.EnableReviewBody(reviewBodyDto.Id);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyDto>>();
        result.IsSuccessStatusCode.ShouldBeTrue();

        // Verify
        client.Verify(c => c.EnableReviewBody(reviewBodyDto.Id), Times.Once());
    }
}
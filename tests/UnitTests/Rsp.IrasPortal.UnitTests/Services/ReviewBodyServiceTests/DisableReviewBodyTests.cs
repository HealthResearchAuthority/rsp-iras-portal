using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ReviewBodyServiceTests;

public class DisableReviewBodyTests : TestServiceBase<ReviewBodyService>
{
    [Theory]
    [AutoData]
    public async Task DisableReviewBody_Should_Return_Success(
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
            .Setup(c => c.DisableReviewBody(It.IsAny<Guid>()))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.DisableReviewBody(reviewBodyDto.Id);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyDto>>();
        result.IsSuccessStatusCode.ShouldBeTrue();

        // Verify
        client.Verify(c => c.DisableReviewBody(reviewBodyDto.Id), Times.Once());
    }
}
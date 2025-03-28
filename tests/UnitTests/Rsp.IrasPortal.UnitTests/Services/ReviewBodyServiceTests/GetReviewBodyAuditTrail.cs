using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

public class GetReviewBodyAuditTrail
{
    [Theory, AutoData]
    public async Task GetReviewBodieyAuditTrail(Guid id)
    {
        // Arrange
        var skip = 0;
        var take = 10;
        var apiResponse = Mock.Of<IApiResponse<ReviewBodyAuditTrailResponse>>(
            apiResponse => !apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.NotFound);

        var client = new Mock<IReviewBodyServiceClient>();
        client.Setup(c => c.GetReviewBodyAuditTrail(id, skip, take))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        // Act
        var result = await sut.ReviewBodyAuditTrail(id, skip, take);

        // Assert
        result.ShouldBeOfType<ServiceResponse<ReviewBodyAuditTrailResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        client.Verify(c => c.GetReviewBodyAuditTrail(id, skip, take), Times.Once());
    }
}
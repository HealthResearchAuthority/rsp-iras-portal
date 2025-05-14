using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.ReviewBodyServiceTests;

public class GetReviewBodyAuditTrail : TestServiceBase<ReviewBodyService>
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

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
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
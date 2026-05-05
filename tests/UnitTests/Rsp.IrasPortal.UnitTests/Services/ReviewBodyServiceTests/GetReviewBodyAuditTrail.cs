using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.ReviewBodyServiceTests;

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

    [Theory, AutoData]
    public async Task GetReviewBodyUserAuditTrail(Guid id, Guid userId)
    {
        const int skip = 0;
        const int take = 10;
        const string sortField = nameof(ReviewBodyAuditTrailDto.DateTimeStamp);
        const string sortDirection = nameof(SortDirection.Descending);

        var apiResponse = Mock.Of<IApiResponse<ReviewBodyAuditTrailResponse>>(
            apiResponse => apiResponse.IsSuccessStatusCode &&
                           apiResponse.StatusCode == HttpStatusCode.OK);

        var client = Mocker.GetMock<IReviewBodyServiceClient>();
        client.Setup(c => c.GetReviewBodyUserAuditTrail(id, userId, skip, take, sortField, sortDirection))
            .ReturnsAsync(apiResponse);

        var sut = new ReviewBodyService(client.Object);

        var result = await sut.ReviewBodyUserAuditTrail(id, userId, skip, take, sortField, sortDirection);

        result.ShouldBeOfType<ServiceResponse<ReviewBodyAuditTrailResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        client.Verify(c => c.GetReviewBodyUserAuditTrail(id, userId, skip, take, sortField, sortDirection), Times.Once());
    }
}
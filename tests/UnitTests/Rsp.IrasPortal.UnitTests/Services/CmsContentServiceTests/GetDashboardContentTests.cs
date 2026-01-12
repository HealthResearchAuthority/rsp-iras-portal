using Microsoft.Extensions.Caching.Memory;
using Rsp.Portal.Application.DTOs.Responses.CmsContent;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.CmsContentServiceTests;

public class GetDashboardContentTests : TestServiceBase<CmsContentService>
{
    private readonly Mock<ICmsContentServiceClient> _cmsClient;

    public GetDashboardContentTests()
    {
        _cmsClient = Mocker.GetMock<ICmsContentServiceClient>();
    }

    [Fact]
    public async Task GetContent_ShouldReturnResponse_From_Service_Not_Cache()
    {
        // Arrange
        var requestUrl = "DashboardContent";
        var apiResponse = new ApiResponse<MixedContentPageResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new MixedContentPageResponse(),
            new()
        );

        Mocker
            .GetMock<ICmsContentServiceClient>()
            .Setup(client => client.GetDashboardContent(false))
            .ReturnsAsync(apiResponse);

        object value;
        Mocker.GetMock<IMemoryCache>()
            .Setup(cache => cache.TryGetValue(requestUrl, out value))
            .Returns(false);

        var mockEntry = new Mock<ICacheEntry>();
        Mocker.GetMock<IMemoryCache>()
           .Setup(cache => cache.CreateEntry(It.IsAny<object>()))
           .Returns(mockEntry.Object);

        // Act
        var result = await Sut.GetDashboardContent();

        // Assert
        result.ShouldBeOfType<ServiceResponse<MixedContentPageResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _cmsClient.Verify(client => client.GetDashboardContent(false), Times.Once());
    }

    [Fact]
    public async Task GetContent_ShouldReturnResponseFromCache_WhenPageIsFoundInCache()
    {
        // Arrange
        var requestUrl = "DashboardContent";
        var apiResponse = new ApiResponse<MixedContentPageResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new MixedContentPageResponse(),
            new()
        );

        object value = apiResponse;
        Mocker.GetMock<IMemoryCache>()
            .Setup(cache => cache.TryGetValue(requestUrl, out value))
            .Returns(true);

        var mockEntry = new Mock<ICacheEntry>();
        mockEntry.SetupAllProperties();
        mockEntry.SetupProperty(x => x.Value, apiResponse);

        Mocker.GetMock<IMemoryCache>()
           .Setup(cache => cache.CreateEntry(requestUrl))
           .Returns(mockEntry.Object);

        // Act
        var result = await Sut.GetDashboardContent();

        // Assert
        result.ShouldBeOfType<ServiceResponse<MixedContentPageResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _cmsClient.Verify(client => client.GetDashboardContent(false), Times.Never);
    }
}
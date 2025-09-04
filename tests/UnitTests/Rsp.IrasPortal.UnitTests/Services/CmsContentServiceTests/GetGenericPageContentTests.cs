using Microsoft.Extensions.Caching.Memory;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.RtsServiceTests;

public class GetGenericPageContentTests : TestServiceBase<CmsContentService>
{
    private readonly Mock<ICmsContentServiceClient> _cmsClient;

    public GetGenericPageContentTests()
    {
        _cmsClient = Mocker.GetMock<ICmsContentServiceClient>();
    }

    [Fact]
    public async Task GetGenericPageContentByUrl_ShouldReturnResponse_WhenPageIsFound()
    {
        // Arrange
        var requestUrl = "/pages/destination-url/";
        var apiResponse = new ApiResponse<GenericPageResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new GenericPageResponse(),
            new()
        );

        Mocker
            .GetMock<ICmsContentServiceClient>()
            .Setup(client => client.GetPageContentByUrl(requestUrl))
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
        var result = await Sut.GetPageContentByUrl(requestUrl);

        // Assert
        result.ShouldBeOfType<ServiceResponse<GenericPageResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _cmsClient.Verify(client => client.GetPageContentByUrl(requestUrl), Times.Once());
    }

    [Fact]
    public async Task GetGenericPageContentByUrl_ShouldReturnResponseFromCache_WhenPageIsFoundInCache()
    {
        // Arrange
        var requestUrl = "/pages/destination-url-cached/";
        var apiResponse = new ApiResponse<GenericPageResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new GenericPageResponse(),
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
        var result = await Sut.GetPageContentByUrl(requestUrl);

        // Assert
        result.ShouldBeOfType<ServiceResponse<GenericPageResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _cmsClient.Verify(client => client.GetPageContentByUrl(requestUrl), Times.Never);
    }
}
using Microsoft.Extensions.Caching.Memory;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.CmsContentServiceTests;

public class GeLoginLandingPageContentTests : TestServiceBase<CmsContentService>
{
    private readonly Mock<ICmsContentServiceClient> _cmsClient;

    public GeLoginLandingPageContentTests()
    {
        _cmsClient = Mocker.GetMock<ICmsContentServiceClient>();
    }

    [Fact]
    public async Task GetContent_ShouldReturnResponse_From_Service_Not_Cache()
    {
        // Arrange
        var requestUrl = "LoginLandingContent";
        var apiResponse = new ApiResponse<GenericPageResponse>
        (
            new HttpResponseMessage(HttpStatusCode.OK),
            new GenericPageResponse(),
            new()
        );

        Mocker
            .GetMock<ICmsContentServiceClient>()
            .Setup(client => client.GetHomeContent(false))
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
        var result = await Sut.GetHomeContent(false);

        // Assert
        result.ShouldBeOfType<ServiceResponse<GenericPageResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _cmsClient.Verify(client => client.GetHomeContent(false), Times.Once());
    }

    [Fact]
    public async Task GetContent_ShouldReturnResponseFromCache_WhenPageIsFoundInCache()
    {
        // Arrange
        var requestUrl = "LoginLandingContent";
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
        var result = await Sut.GetHomeContent();

        // Assert
        result.ShouldBeOfType<ServiceResponse<GenericPageResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _cmsClient.Verify(client => client.GetHomeContent(false), Times.Never);
    }
}
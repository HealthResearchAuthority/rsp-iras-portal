using Rsp.Portal.Services.Extensions;

namespace Rsp.Portal.UnitTests.Services.ApiResponseExtensionsTest;

public class ToServiceResponse : TestServiceBase
{
    [Theory, AutoData]
    public void Should_ReturnServiceResponse_Of_T_WithContent_When_ApiResponseIsSuccessful_And_IncludeContentIsTrue
    (
        object content
    )
    {
        // Arrange
        var apiResponse = new Mock<IApiResponse<object>>();

        apiResponse
            .SetupGet(x => x.IsSuccessStatusCode)
            .Returns(true);

        apiResponse
            .SetupGet(x => x.Content)
            .Returns(content);

        // Act
        var result =
            apiResponse.Object.ToServiceResponse(includeContent: true);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldBe(content);
    }

    [Fact]
    public void Should_ReturnServiceResponse_Of_T_WithoutContent_When_ApiResponseIsSuccessful_And_IncludeContentIsFalse()
    {
        // Arrange
        var apiResponse = new Mock<IApiResponse<object>>();

        apiResponse
            .SetupGet(x => x.IsSuccessStatusCode)
            .Returns(true);

        // Act
        var result =
            apiResponse.Object.ToServiceResponse(includeContent: false);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldBeNull();
    }

    [Theory, AutoData]
    public async Task Should_ReturnServiceResponse_Of_T_WithError_When_ApiResponseIsNotSuccessful
    (
        string reasonPhrase, string errorMessage, HttpStatusCode statusCode = HttpStatusCode.UnprocessableContent
    )
    {
        // Arrange
        var apiResponse = new Mock<IApiResponse<object>>();

        // create a failed response
        var responseMessage = new HttpResponseMessage(statusCode)
        {
            ReasonPhrase = reasonPhrase,
            StatusCode = statusCode,
            Content = new StringContent(errorMessage)
        };

        // compose ApiResponse with ApiException
        var apiException = await ApiException
            .Create(new HttpRequestMessage(), It.IsAny<HttpMethod>(), responseMessage, new());

        apiResponse
            .SetupGet(x => x.Error)
            .Returns(apiException);

        apiResponse
            .SetupGet(x => x.ReasonPhrase)
            .Returns(reasonPhrase);

        apiResponse
            .SetupGet(x => x.StatusCode)
            .Returns(statusCode);

        // Act
        var result = apiResponse.Object.ToServiceResponse(includeContent: false);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.Error!.ShouldContain(errorMessage);
        result.ReasonPhrase.ShouldBe(reasonPhrase);
        result.StatusCode.ShouldBe(statusCode);
    }

    [Fact]
    public void Should_ReturnServiceResponse_When_ApiResponseIsSuccessful()
    {
        // Arrange
        var apiResponse = new Mock<IApiResponse>();

        apiResponse
            .SetupGet(x => x.IsSuccessStatusCode)
            .Returns(true);

        // Act
        var result =
            apiResponse.Object.ToServiceResponse();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Theory, AutoData]
    public async Task Should_ReturnServiceResponseWithError_When_ApiResponseIsNotSuccessful
    (
        string errorMessage, string reasonPhrase, HttpStatusCode statusCode
    )
    {
        // Arrange
        var apiResponse = new Mock<IApiResponse>();

        // create a failed response
        var responseMessage = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(errorMessage),
            ReasonPhrase = reasonPhrase
        };

        // compose ApiResponse with ApiException
        var apiException = await ApiException
            .Create(new HttpRequestMessage(), It.IsAny<HttpMethod>(), responseMessage, new());

        apiResponse
            .SetupGet(x => x.Error)
            .Returns(apiException);

        apiResponse
            .SetupGet(x => x.StatusCode)
            .Returns(statusCode);

        // Act
        var result = apiResponse.Object.ToServiceResponse();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.Error!.ShouldContain(errorMessage);
        result.ReasonPhrase.ShouldBe(reasonPhrase);
        result.StatusCode.ShouldBe(statusCode);
    }
}
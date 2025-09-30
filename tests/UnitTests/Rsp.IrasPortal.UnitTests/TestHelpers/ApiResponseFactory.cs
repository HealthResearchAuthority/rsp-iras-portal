namespace Rsp.IrasPortal.UnitTests.TestHelpers;

internal static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(T content)
    {
        var message = new HttpResponseMessage(HttpStatusCode.OK)
        {
            ReasonPhrase = "OK"
        };

        return new ApiResponse<T>(message, content, new RefitSettings());
    }

    public static IApiResponse Success()
    {
        return Mock.Of<IApiResponse>
        (
            apiRespone =>
                apiRespone.StatusCode == HttpStatusCode.OK &&
                apiRespone.ReasonPhrase == "OK"
        );
    }

    public static ApiResponse<T> Failure<T>(HttpStatusCode statusCode, string reason, string body)
    {
        var message = new HttpResponseMessage(statusCode)
        {
            ReasonPhrase = reason,
            Content = new StringContent(body)
        };

        var exception = ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, message, new()).GetAwaiter().GetResult();
        return new ApiResponse<T>(message, default, new RefitSettings(), exception);
    }

    public static IApiResponse Failure(HttpStatusCode statusCode, string reason)
    {
        return Mock.Of<IApiResponse>
        (
            apiRespone =>
                apiRespone.StatusCode == statusCode &&
                apiRespone.ReasonPhrase == reason
        );
    }
}
using Refit;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Services.Extensions;

public static class ApiResponseExtensions
{
    public static ServiceResponse<T> ToServiceResponse<T>(this IApiResponse<T> apiResponse, bool includeContent = true)
    {
        var serviceResponse = new ServiceResponse<T>();

        return (apiResponse.IsSuccessStatusCode, includeContent) switch
        {
            (true, true) =>
                serviceResponse
                    .WithStatus()
                    .WithContent(apiResponse.Content!),
            (true, false) =>
                serviceResponse
                    .WithStatus(),
            _ => serviceResponse
                    .WithError
                    (
                        apiResponse.Error?.Content,
                        apiResponse.Error?.ReasonPhrase,
                        apiResponse.StatusCode
                    )
        };
    }

    public static ServiceResponse ToServiceResponse(this IApiResponse apiResponse)
    {
        var serviceResponse = new ServiceResponse();

        return apiResponse.IsSuccessStatusCode switch
        {
            true =>
                serviceResponse
                    .WithStatus(),
            _ => serviceResponse
                    .WithError
                    (
                        apiResponse.Error?.Content,
                        apiResponse.Error?.ReasonPhrase,
                        apiResponse.StatusCode
                    )
        };
    }
}
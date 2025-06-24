using Microsoft.AspNetCore.Http;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

public interface IFileStorageService
{
    Task<ServiceResponse<string>> UploadFileAsync(IFormFile file, IProgress<long>? progress = null);
}
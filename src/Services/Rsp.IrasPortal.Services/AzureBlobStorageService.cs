using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Services;

public class AzureBlobStorageService(BlobServiceClient blobServiceClient) : IFileStorageService
{
    public async Task<ServiceResponse<string>> UploadFileAsync(IFormFile file, IProgress<long>? progress = null)
    {
        if (file == null || file.Length == 0)
        {
            return new ServiceResponse<string>
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                ReasonPhrase = "File is empty or null.",
            };
        }

        if (blobServiceClient == null)
        {
            return new ServiceResponse<string>
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                ReasonPhrase = "BlobServiceClient is not initialized.",
            };
        }

        var containerClient = blobServiceClient.GetBlobContainerClient("documentupload");
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(Guid.NewGuid() + file.FileName);

        var options = new BlobUploadOptions
        {
            ProgressHandler = progress
        };

        await blobClient.UploadAsync(file.OpenReadStream(), options);

        return new ServiceResponse<string>
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = blobClient.Uri.ToString(),
        };
    }
}
using System.Net;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Services;

/// <summary>
/// Service responsible for managing file uploads and retrievals from Azure Blob Storage.
/// </summary>
/// <param name="blobServiceClient">Injected Azure BlobServiceClient for interacting with the Blob service.</param>
public class BlobStorageService(BlobServiceClient blobServiceClient) : IBlobStorageService
{
    /// <summary>
    /// Uploads a collection of files to the specified Azure Blob Storage container and folder path.
    /// </summary>
    /// <param name="files">The collection of files to upload.</param>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="folderPrefix">The prefix to prepend to each blob name (typically the folder path).</param>
    /// <returns>A list of <see cref="DocumentSummaryItemDto"/> containing metadata about each uploaded file.</returns>
    /// <exception cref="ArgumentException">Thrown if the file list is null or empty.</exception>
    public async Task<List<DocumentSummaryItemDto>> UploadFilesAsync(IEnumerable<IFormFile> files, string containerName, string folderPrefix)
    {
        if (files == null || !files.Any())
            throw new ArgumentException("No files to upload.", nameof(files));

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var uploadedBlobs = new List<DocumentSummaryItemDto>();

        foreach (var file in files)
        {
            // Generate a unique blob name using GUID to prevent collisions
            var blobName = $"{folderPrefix}/{Guid.NewGuid()}_{file.FileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, overwrite: true);

            uploadedBlobs.Add(new DocumentSummaryItemDto
            {
                BlobUri = blobName,
                FileName = file.FileName,
                FileSize = file.Length
            });
        }

        return uploadedBlobs;
    }

    /// <summary>
    /// Lists all files in a specific folder (prefix) within a given blob container.
    /// </summary>
    /// <param name="containerName">The name of the blob container to search within.</param>
    /// <param name="folderPrefix">The folder prefix used to filter blobs.</param>
    /// <returns>A list of <see cref="DocumentSummaryItemDto"/> representing the files found.</returns>
    public async Task<List<DocumentSummaryItemDto>> ListFilesAsync(string containerName, string folderPrefix)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var results = new List<DocumentSummaryItemDto>();

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: folderPrefix))
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            results.Add(new DocumentSummaryItemDto
            {
                FileName = Path.GetFileName(blobItem.Name),
                BlobUri = blobClient.Uri.ToString(),
                FileSize = blobItem.Properties.ContentLength.GetValueOrDefault()
            });
        }

        return results;
    }

    /// <summary>
    /// Uploads a collection of files to the specified Azure Blob Storage container and folder path.
    /// </summary>
    /// <param name="files">The collection of files to upload.</param>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="folderPrefix">The prefix to prepend to each blob name (typically the folder path).</param>
    /// <returns>A list of <see cref="DocumentSummaryItemDto"/> containing metadata about each uploaded file.</returns>
    /// <exception cref="ArgumentException">Thrown if the file list is null or empty.</exception>
    public async Task<ServiceResponse> DeleteFileAsync(string containerName, string blobPath)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return new ServiceResponse { StatusCode = HttpStatusCode.BadRequest, Error = "Blob path cannot be null or empty." };
        }

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobPath);

        // Delete the blob if it exists
        await blobClient.DeleteIfExistsAsync();

        return new ServiceResponse { StatusCode = HttpStatusCode.OK };
    }

    public async Task<ServiceResponse<IActionResult>> DownloadFileToHttpResponseAsync
    (
        string containerName,
        string blobPath,
        string fileName
    )
    {
        var response = new ServiceResponse<IActionResult>();

        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return response.WithContent(
                new BadRequestObjectResult("Blob path cannot be null or empty."),
                HttpStatusCode.BadRequest
            );
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return response.WithContent(
                new BadRequestObjectResult("File name cannot be null or empty."),
                HttpStatusCode.BadRequest
            );
        }

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync())
        {
            return response.WithContent(
                new NotFoundObjectResult($"File not found at path '{blobPath}'."),
                HttpStatusCode.NotFound
            );
        }

        var properties = await blobClient.GetPropertiesAsync();
        var downloadResponse = await blobClient.DownloadStreamingAsync();

        var fileResult = new FileStreamResult(
            downloadResponse.Value.Content,
            properties.Value.ContentType ?? "application/octet-stream")
        {
            FileDownloadName = fileName
        };

        return response.WithContent(fileResult, HttpStatusCode.OK);
    }
}
using System.IO.Compression;
using System.Net;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;

namespace Rsp.Portal.Services;

/// <summary>
/// Service responsible for managing file uploads and retrievals from Azure Blob Storage.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    /// <summary>
    /// Uploads a collection of files to the specified Azure Blob Storage container and folder path.
    /// </summary>
    /// <param name="files">The collection of files to upload.</param>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="folderPrefix">The prefix to prepend to each blob name (typically the folder path).</param>
    /// <returns>A list of <see cref="DocumentSummaryItemDto"/> containing metadata about each uploaded file.</returns>
    /// <exception cref="ArgumentException">Thrown if the file list is null or empty.</exception>
    public async Task<List<DocumentSummaryItemDto>> UploadFilesAsync(BlobServiceClient blobServiceClient, IEnumerable<IFormFile> files, string containerName, string folderPrefix)
    {
        if (files == null || !files.Any())
            throw new ArgumentException("No files to upload.", nameof(files));

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var uploadedBlobs = new List<DocumentSummaryItemDto>();

        foreach (var file in files)
        {
            // Generate a unique blob name using GUID to prevent collisions
            var blobName = $"{folderPrefix}/{file.FileName}";
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
    public async Task<List<DocumentSummaryItemDto>> ListFilesAsync(BlobServiceClient blobServiceClient, string containerName, string folderPrefix)
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
    public async Task<ServiceResponse> DeleteFileAsync(BlobServiceClient blobServiceClient, string containerName, string blobPath)
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
        BlobServiceClient blobServiceClient,
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

    public async Task<(byte[] FileBytes, string FileName)> DownloadFolderAsZipAsync(
    BlobServiceClient blobServiceClient,
    string containerName,
    string folderName,
    string saveAsFileName)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Ensure folderName ends with slash
        if (!folderName.EndsWith("/"))
            folderName += "/";

        // List all blobs under folder prefix
        var blobs = containerClient.GetBlobsAsync(prefix: folderName.ToLower());

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            await foreach (var blobItem in blobs)
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);

                using var blobContent = new MemoryStream();
                await blobClient.DownloadToAsync(blobContent);
                blobContent.Position = 0;

                // Remove the folderName prefix to get just the filename (or subpath)
                string entryName = blobItem.Name.Substring(folderName.Length);

                var zipEntry = archive.CreateEntry(entryName, CompressionLevel.Fastest);

                using var entryStream = zipEntry.Open();
                blobContent.CopyTo(entryStream);
            }
        }

        var finalBytes = zipStream.ToArray();
        var zipName = $"{saveAsFileName}.zip";

        return (finalBytes, zipName);
    }
}
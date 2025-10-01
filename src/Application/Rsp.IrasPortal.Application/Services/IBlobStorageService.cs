using Microsoft.AspNetCore.Http;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

/// <summary>
/// Defines operations for interacting with Azure Blob Storage for uploading and listing documents.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads one or more files to the specified Azure Blob Storage container and folder.
    /// </summary>
    /// <param name="files">The collection of files to be uploaded.</param>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="folderPrefix">The folder path or prefix to organize blobs within the container (e.g., a project or user identifier).</param>
    /// <returns>
    /// A list of <see cref="DocumentSummaryItemDto"/> containing metadata for each uploaded file, such as file name, URI, and size.
    /// </returns>
    Task<List<DocumentSummaryItemDto>> UploadFilesAsync(IEnumerable<IFormFile> files, string containerName, string folderPrefix);

    /// <summary>
    /// Lists all files within a specific folder (prefix) inside the given blob container.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="folderPrefix">The folder path or prefix under which files are stored.</param>
    /// <returns>
    /// A list of <see cref="DocumentSummaryItemDto"/> representing the files found in the specified folder.
    /// </returns>
    Task<List<DocumentSummaryItemDto>> ListFilesAsync(string containerName, string folderPrefix);

    Task<ServiceResponse> DeleteFileAsync(string containerName, string blobPath);
}
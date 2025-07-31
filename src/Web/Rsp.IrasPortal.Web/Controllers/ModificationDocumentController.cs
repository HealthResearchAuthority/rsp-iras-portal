using System.Net;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

public partial class ProjectModificationController : Controller
{
    private const string ContainerName = "staging";

    /// <summary>
    /// Returns the view for uploading project documents.
    /// Populates metadata from TempData.
    /// </summary>
    [HttpGet]
    public IActionResult ProjectDocument()
    {
        var specificAreaOfChange = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string;

        var viewModel = new ModificationUploadDocumentsViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = !string.IsNullOrEmpty(specificAreaOfChange)
            ? $"Add documents for {specificAreaOfChange}"
            : string.Empty
        };

        // Retrieve the current organisation search term from TempData
        return View(nameof(UploadDocuments), viewModel);
    }

    /// <summary>
    /// Handles the upload of project modification documents to blob storage.
    /// Saves metadata to the database and redirects to the review page.
    /// </summary>
    /// <param name="model">The model containing files to upload and project-related identifiers.</param>
    /// <returns>A redirection to the review view if upload is successful; otherwise, returns the current view with validation errors.</returns>
    [HttpPost]
    public async Task<IActionResult> UploadDocuments(ModificationUploadDocumentsViewModel model)
    {
        // Validate file input
        if (!ModelState.IsValid || model.Files == null || !model.Files.Any())
        {
            ModelState.AddModelError("Files", "Please upload at least one document.");
            return View(model);
        }

        // Upload files to blob storage and return metadata (URIs, sizes, etc.)
        var uploadedBlobs = await blobStorageService.UploadFilesAsync(
            model.Files,
            ContainerName,
            model.IrasId.ToString());

        // Retrieve contextual identifiers from TempData and HttpContext
        var projectModificationChangeId = TempData.Peek(TempDataKeys.ProjectModificationChangeId);
        var respondentId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!;
        var projectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty;

        // Map uploaded blob metadata to document request DTOs
        var uploadedDocuments = new List<ProjectModificationDocumentRequest>();
        foreach (var uploadedBlob in uploadedBlobs)
        {
            var document = new ProjectModificationDocumentRequest
            {
                ProjectModificationChangeId = projectModificationChangeId == null ? Guid.Empty : (Guid)projectModificationChangeId!,
                ProjectRecordId = projectRecordId,
                ProjectPersonnelId = respondentId,
                FileName = uploadedBlob.FileName,
                DocumentStoragePath = uploadedBlob.BlobUri,
                FileSize = uploadedBlob.FileSize
            };
            uploadedDocuments.Add(document);
        }

        // Save uploaded document metadata to the backend service
        await projectModificationsService.CreateDocumentModification(uploadedDocuments);

        TempData["UploadSuccess"] = true;
        return RedirectToAction(nameof(ReviewDocument));
    }

    /// <summary>
    /// Displays the review page for uploaded project modification documents.
    /// Fetches document metadata from the backend service.
    /// </summary>
    /// <returns>The review view with the list of uploaded documents or an error message if none found.</returns>
    [HttpGet]
    public async Task<IActionResult> ReviewDocument()
    {
        // Fetch contextual data for the view
        var specificAreaOfChange = TempData.Peek(TempDataKeys.SpecificAreaOfChangeText) as string;

        var viewModel = new ModificationReviewDocumentsViewModel
        {
            ShortTitle = TempData.Peek(TempDataKeys.ShortProjectTitle) as string ?? string.Empty,
            IrasId = TempData.Peek(TempDataKeys.IrasId)?.ToString() ?? string.Empty,
            ModificationIdentifier = TempData.Peek(TempDataKeys.ProjectModificationIdentifier) as string ?? string.Empty,
            PageTitle = !string.IsNullOrEmpty(specificAreaOfChange)
                ? $"Documents added for {specificAreaOfChange}"
                : string.Empty
        };

        // Create request for fetching documents
        var documentChangeRequest = new ProjectModificationDocumentRequest
        {
            ProjectModificationChangeId = (Guid)TempData.Peek(TempDataKeys.ProjectModificationChangeId)!,
            ProjectRecordId = TempData.Peek(TempDataKeys.ProjectRecordId) as string ?? string.Empty,
            ProjectPersonnelId = (HttpContext.Items[ContextItemKeys.RespondentId] as string)!,
        };

        // Call the respondent service to retrieve uploaded documents
        var response = await respondentService.GetModificationChangesDocuments(
            documentChangeRequest.ProjectModificationChangeId,
            documentChangeRequest.ProjectRecordId,
            documentChangeRequest.ProjectPersonnelId);

        if (response?.StatusCode == HttpStatusCode.OK && response.Content != null)
        {
            viewModel.UploadedDocuments = response.Content.Select(
                a => new DocumentSummaryItemDto
                {
                    FileName = a.FileName,
                    FileSize = a.FileSize ?? 0,
                    BlobUri = a.DocumentStoragePath
                }).ToList();
        }
        else
        {
            // Handle the case where no documents were returned or service failed
            viewModel.UploadedDocuments = new List<DocumentSummaryItemDto>();
            ModelState.AddModelError(string.Empty, "No documents found or an error occurred while retrieving documents.");
        }

        return View("ModificationReviewDocuments", viewModel);
    }

    [HttpPost]
    public IActionResult AddAnotherDocument()
    {
        return RedirectToAction(nameof(UploadDocuments));
    }
}